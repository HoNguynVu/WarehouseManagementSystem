using API.Consumers;
using Application.Features.Orders.Commands.CreateOrder;
using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Application.Sagas;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWorks;
using Infrastructure.Sagas;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SharedLibrary.Middlewares;
using SharedLibrary.Responses;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/order-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Order Service API...");

    var builder = WebApplication.CreateBuilder(args);

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
    builder.Services.AddSwaggerGen();

    // MassTransit Saga & Consumers Configuration
    builder.Services.AddMassTransit(x =>
    {
        // Register Saga State Machine
        x.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.ExistingDbContext<OrderDbContext>();
            });

        // Register Consumers
        x.AddConsumer<UpdateOrderStatusConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h => {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    // AutoMapper
    builder.Services.AddAutoMapper(config =>
    {
        config.AddProfile<Application.Mappings.OrderProfile>();
    });

    // DbContext
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
            };
        });

    // Settings & HttpClient
    builder.Services.Configure<ZaloPaySettings>(builder.Configuration.GetSection("ZaloPaySettings"));
    builder.Services.AddHttpClient();

    // MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(AppDomain.CurrentDomain.Load("Application")));

    // DI
    builder.Services.AddScoped<IOrderUow, UnitOfWorks>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();

    var app = builder.Build();

    app.UseGlobalException();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    // app.UseAuthentication();
    // app.UseAuthorization();
    app.MapControllers();

    // Auto-migration on startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.Migrate();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Service API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
