using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Settings;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using SharedLibrary.Middlewares;
using SharedLibrary.Observability;
using SharedLibrary.Responses;
using Microsoft.OpenApi.Models;
using Domain.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Catalog Service API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWmsSerilog(builder.Configuration, "CatalogService", "Logs/catalog-log-.txt");

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Add services to the container.

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var response = ApiResponse<object>.Failure("Dữ liệu không hợp lệ", 400, errors);

                return new BadRequestObjectResult(response);
            };
        });
        
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Nhập token theo chuẩn: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });

    // MassTransit & RabbitMQ
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h => {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.UseCorrelationId(context);
        });
    });

    builder.Services.AddCorrelationIdPropagation();

    // MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(AppDomain.CurrentDomain.Load("Application")));

    // AutoMapper
    builder.Services.AddAutoMapper(config =>
    {
        config.AddProfile<Application.Mappings.CatalogProfile>();
    });

    // Redis Cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:Host"] ?? "localhost:6379";
        options.InstanceName = "CatalogSystem_";
    });

    // MongoDB
    builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
    builder.Services.AddSingleton<IMongoClient>(sp =>
    {
        var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        return new MongoClient(settings.ConnectionString);
    });

    builder.Services.AddScoped<CatalogContext>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

    var app = builder.Build();

    app.UseCorrelationId();
    app.UseGlobalException();
    
    app.UseCors("CorsPolicy");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Catalog Service API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

