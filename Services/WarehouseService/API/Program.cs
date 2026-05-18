using API.Consumers;
using Application.Interfaces;
using Application.Services;
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
using SharedLibrary.IntergrationEvents;

//Add Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Ghi ra màn hình Console
    .WriteTo.File("Logs/warehouse-log-.txt", rollingInterval: RollingInterval.Day) // Mỗi ngày tạo 1 file log riêng
    .CreateLogger();

try
{
    Log.Information("Starting Warehouse Service API...");
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // Lấy danh sách các lỗi validation
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Đóng gói vào chuẩn ApiResponse của Vinh
                var response = ApiResponse<object>.Failure("Dữ liệu không hợp lệ", 400, errors);

                return new BadRequestObjectResult(response);
            };
        });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Warehouse API", Version = "v1" });

        //Thêm nút Authorize 
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
                Array.Empty<string>()
            }
        });
    });

    // Add HttpClient 
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

    builder.Services.AddDbContext<WarehouseDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("WarehouseDb")));

    builder.Services.AddAutoMapper(config =>
    {
        config.AddProfile<Application.Mappings.WarehouseProfile>();
    });

    builder.Services.AddMassTransit(x =>
    {
        // 1. Đăng ký cái đài lắng nghe
        x.AddConsumer<OrderAllocatedConsumer>();
        x.AddConsumer<ProductUpdatedConsumer>();
        // 2. Kết nối tới Bưu điện RabbitMQ
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            // 3. Tự động cấu hình các endpoint (hòm thư) dựa trên tên của Consumer
            cfg.ConfigureEndpoints(context);
        });
    });

    //Cấu hình Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost:6379"; 
        options.InstanceName = "WarehouseSystem_";
    });

    builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
    builder.Services.AddScoped<IWarehouseService, WarehouseService>();
    builder.Services.AddScoped<IWarehouseUow, WarehouseUow>();

    var app = builder.Build();

    // Migrate database automatically
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        dbContext.Database.Migrate();
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

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
