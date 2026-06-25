using API.Consumers;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SharedLibrary.Responses;
using MassTransit;
using Serilog;
using SharedLibrary.IntegrationEvents;
using System.Reflection;
using FluentValidation;
using SharedLibrary.Middlewares;
using Infrastructure.Data.Interceptors;
using SharedLibrary.Observability;

//Add Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Ghi ra mÃ n hÃ¬nh Console
    .WriteTo.File("Logs/warehouse-log-.txt", rollingInterval: RollingInterval.Day) // Má»—i ngÃ y táº¡o 1 file log riÃªng
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Warehouse Service API...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWmsSerilog(builder.Configuration, "WarehouseService", "Logs/warehouse-log-.txt");

    // Add services to the container.

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // Láº¥y danh sÃ¡ch cÃ¡c lá»—i validation
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // ÄÃ³ng gÃ³i vÃ o chuáº©n ApiResponse cá»§a Vinh
                var response = ApiResponse<object>.Failure("Dá»¯ liá»‡u khÃ´ng há»£p lá»‡", 400, errors);

                return new BadRequestObjectResult(response);
            };
        });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Warehouse API", Version = "v1" });

        //ThÃªm nÃºt Authorize 
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Nháº­p token theo chuáº©n: Bearer {token}",
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
                Array.Empty<string>()
            }
        });
    });

    // Add HttpClient 
    builder.Services.AddCorrelationIdPropagation();
    builder.Services.AddHttpClient("CatalogClient", client =>
    {
        var catalogUrl = builder.Configuration["CatalogApiUrl"];
        client.BaseAddress = new Uri(catalogUrl!);
    });

    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    //Add Jwt Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });

    builder.Services.AddScoped<AuditableEntityInterceptor>();
    builder.Services.AddScoped<DispatchDomainEventsInterceptor>();

    builder.Services.AddDbContext<WarehouseDbContext>((sp, options) =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("WarehouseDb"))
               .AddInterceptors(
                   sp.GetRequiredService<AuditableEntityInterceptor>(),
                   sp.GetRequiredService<DispatchDomainEventsInterceptor>());
    });

    builder.Services.AddValidatorsFromAssembly(AppDomain.CurrentDomain.Load("Application"));

    builder.Services.AddAutoMapper(config =>
    {
        config.AddProfile<Application.Mappings.WarehouseProfile>();
    });

    builder.Services.AddMassTransit(x =>
    {
        // ÄÄƒng kÃ½ Outbox Ä‘á»ƒ Ä‘áº£m báº£o tÃ­nh nháº¥t quÃ¡n khi gá»­i sá»± kiá»‡n ra ngoÃ i sau khi Ä‘Ã£ cáº­p nháº­t database thÃ nh cÃ´ng
        x.AddEntityFrameworkOutbox<WarehouseDbContext>(o =>
        {
            // QuÃ©t database má»—i giÃ¢y Ä‘á»ƒ xem cÃ³ thÆ° nÃ o chÆ°a gá»­i thÃ¬ gá»­i Ä‘i
            o.QueryDelay = TimeSpan.FromSeconds(1);

            // Khai bÃ¡o loáº¡i Database Ä‘ang dÃ¹ng
            o.UsePostgres();
            o.UseBusOutbox();
        });

        // 1. ÄÄƒng kÃ½ cÃ¡i Ä‘Ã i láº¯ng nghe
        x.AddConsumer<ProductUpdatedConsumer>();
        x.AddConsumer<AllocateOrderConsumer>();
        x.AddConsumer<ReleaseOrderStockConsumer>();
        // 2. Káº¿t ná»‘i tá»›i BÆ°u Ä‘iá»‡n RabbitMQ
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            // 3. Tá»± Ä‘á»™ng cáº¥u hÃ¬nh cÃ¡c endpoint (hÃ²m thÆ°) dá»±a trÃªn tÃªn cá»§a Consumer
            cfg.UseCorrelationId(context);
            cfg.ConfigureEndpoints(context);
        });
    });

    //Cáº¥u hÃ¬nh Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:Host"] ?? "localhost:6379"; 
        options.InstanceName = "WarehouseSystem_";
    });

    builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
    builder.Services.AddScoped<IWarehouseUow, WarehouseUow>();
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(AppDomain.CurrentDomain.Load("Application"));
        cfg.AddOpenBehavior(typeof(Application.Behaviors.LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(Application.Behaviors.ValidationBehavior<,>));
    });

    var app = builder.Build();

    app.UseCorrelationId();

    // Migrate database automatically
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseGlobalException();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Warehouse Service API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

