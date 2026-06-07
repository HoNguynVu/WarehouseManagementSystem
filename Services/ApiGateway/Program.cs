using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Đọc cấu hình YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Đăng ký Health Checks cho các infrastructure
builder.Services.AddHealthChecks()
    .AddRabbitMQ(sp => new RabbitMQ.Client.ConnectionFactory { Uri = new Uri("amqp://guest:guest@localhost:5672") }.CreateConnectionAsync(), name: "RabbitMQ")
    .AddRedis("localhost:6379", name: "Redis Cache")
    .AddMongoDb(sp => new MongoDB.Driver.MongoClient("mongodb://admin:123@localhost:27018/?authSource=admin"), name: "MongoDB (Catalog)")
    .AddNpgSql("Host=localhost;Port=5433;Database=warehouse_db;Username=admin;Password=123", name: "PostgreSQL (Warehouse)")
    .AddSqlServer("Server=localhost,1435;Database=OrderDb;User Id=sa;Password=Vudz1234;TrustServerCertificate=True", name: "SQL Server (Order)")
    .AddSqlServer("Server=localhost,1434;Database=master;User Id=sa;Password=Vudz1234;TrustServerCertificate=True", name: "SQL Server (Identity)");

// Đăng ký HealthChecks UI
builder.Services.AddHealthChecksUI(setupSettings: setup =>
{
    setup.AddHealthCheckEndpoint("System Health", "/health");
    // Thời gian poll mặc định
    setup.SetEvaluationTimeInSeconds(10);
}).AddInMemoryStorage();

var app = builder.Build();

app.UseCors("CorsPolicy");

// Map endpoint cho Health Check (Trả về file JSON)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Map endpoint cho giao diện UI
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/healthchecks-ui";
});

// Map định tuyến
app.MapReverseProxy();

app.Run();