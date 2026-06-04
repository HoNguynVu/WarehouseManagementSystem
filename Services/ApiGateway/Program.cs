using SharedLibrary.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWmsSerilog(builder.Configuration, "ApiGateway", "Logs/gateway-log-.txt");

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddCorrelationIdPropagation();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseCorrelationId();

app.MapReverseProxy();

app.Run();
