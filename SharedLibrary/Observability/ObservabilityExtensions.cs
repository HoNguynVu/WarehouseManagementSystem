using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Reflection;

namespace SharedLibrary.Observability
{
    public static class ObservabilityExtensions
    {
        public static IHostBuilder UseWmsSerilog(
            this IHostBuilder hostBuilder,
            IConfiguration configuration,
            string serviceName,
            string logFilePath)
        {
            return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
            {
                var resolvedServiceName = configuration["ServiceName"]
                    ?? serviceName
                    ?? Assembly.GetEntryAssembly()?.GetName().Name
                    ?? "WarehouseManagementSystem";

                var seqServerUrl = configuration["Seq:ServerUrl"] ?? "http://localhost:5341";

                loggerConfiguration
                    .ReadFrom.Configuration(configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ServiceName", resolvedServiceName)
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .WriteTo.Console();

                if (!string.IsNullOrWhiteSpace(logFilePath))
                {
                    loggerConfiguration.WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day);
                }

                if (!string.IsNullOrWhiteSpace(seqServerUrl))
                {
                    loggerConfiguration.WriteTo.Seq(seqServerUrl);
                }
            });
        }

        public static IServiceCollection AddCorrelationIdPropagation(this IServiceCollection services)
        {
            services.AddTransient<CorrelationIdDelegatingHandler>();
            services.AddTransient(typeof(CorrelationPublishFilter<>));
            services.AddTransient(typeof(CorrelationSendFilter<>));
            services.AddTransient(typeof(CorrelationConsumeFilter<>));
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    builder.AdditionalHandlers.Add(
                        builder.Services.GetRequiredService<CorrelationIdDelegatingHandler>());
                });
            });

            return services;
        }

        public static IHttpClientBuilder AddCorrelationIdHandler(this IHttpClientBuilder builder)
        {
            return builder.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
        }

        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }

        public static void UseCorrelationId(this IBusFactoryConfigurator configurator, IRegistrationContext context)
        {
            configurator.UsePublishFilter(typeof(CorrelationPublishFilter<>), context);
            configurator.UseSendFilter(typeof(CorrelationSendFilter<>), context);
            configurator.UseConsumeFilter(typeof(CorrelationConsumeFilter<>), context);
        }
    }
}
