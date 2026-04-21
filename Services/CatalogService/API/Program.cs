using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using SharedLibrary.Middlewares;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/catalog-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Catalog Service API...");

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Cấu hình AutoMapper
    builder.Services.AddAutoMapper(config =>
    {
        config.AddProfile<Application.Mappings.CatalogProfile>();
    });

    // Cấu hình Redis Cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = "localhost:6379";
        options.InstanceName = "CatalogSystem_";
    });

    //Đọc cấu hình MongoDB từ appsettings.json và đăng ký dịch vụ
    builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

    //Đăng ký dịch vụ MongoDB Client
    builder.Services.AddSingleton<IMongoClient>(sp =>
    {
        var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
        return new MongoClient(settings.ConnectionString);
    });

    builder.Services.AddScoped<CatalogContext>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();

    var app = builder.Build();

    app.UseGlobalException();

    // Configure the HTTP request pipeline.
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