using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Observability;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using ApiGateway;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWmsSerilog(builder.Configuration, "ApiGateway", "Logs/gateway-log-.txt");

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken: token);
    };

    options.AddPolicy("fixed-window", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10)
            }));
});

builder.Services.AddCorrelationIdPropagation();

// Register Custom Polly Factory for YARP
builder.Services.AddSingleton<IForwarderHttpClientFactory, ResilientForwarderHttpClientFactory>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks()
    .AddRabbitMQ(sp => new RabbitMQ.Client.ConnectionFactory { Uri = new Uri(builder.Configuration["HealthChecks:RabbitMQ"] ?? "amqp://guest:guest@localhost:5672") }.CreateConnectionAsync(), name: "RabbitMQ")
    .AddRedis(builder.Configuration["HealthChecks:Redis"] ?? "localhost:6379", name: "Redis Cache")
    .AddMongoDb(sp => new MongoDB.Driver.MongoClient(builder.Configuration["HealthChecks:MongoDB"] ?? "mongodb://admin:123@localhost:27018/?authSource=admin"), name: "MongoDB (Catalog)")
    .AddNpgSql(builder.Configuration["HealthChecks:PostgreSQL"] ?? "Host=localhost;Port=5433;Database=warehouse_db;Username=admin;Password=123", name: "PostgreSQL (Warehouse)")
    .AddSqlServer(builder.Configuration["HealthChecks:SQLOrder"] ?? "Server=localhost,1435;Database=OrderDb;User Id=sa;Password=Vudz1234;TrustServerCertificate=True", name: "SQL Server (Order)")
    .AddSqlServer(builder.Configuration["HealthChecks:SQLIdentity"] ?? "Server=localhost,1434;Database=master;User Id=sa;Password=Vudz1234;TrustServerCertificate=True", name: "SQL Server (Identity)");

builder.Services.AddHealthChecksUI(setupSettings: setup =>
{
    setup.AddHealthCheckEndpoint("System Health", "http://localhost:8080/health");
    setup.SetEvaluationTimeInSeconds(10);
}).AddInMemoryStorage();

var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseCorrelationId();
app.UseRateLimiter();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/healthchecks-ui";
});

app.MapReverseProxy();

app.Run();



