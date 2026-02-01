using Microsoft.Extensions.Options;
using Order_API.Data;
using Order_API.Hubs;
using Order_API.Service.Orderser;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// Load base + environment config (your custom naming scheme)
builder.Configuration
    .AddJsonFile("Order_API_appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Order_API_appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Hosting / Ports
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (env.IsDevelopment())
        serverOptions.ListenLocalhost(5004);
    else
        serverOptions.ListenAnyIP(80);
});

// Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Mongo config + context
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection(nameof(MongoDBSettings)));

builder.Services.AddSingleton<MongoDBContext>();

// CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = GetAllowedOrigins(builder.Configuration);
    options.AddPolicy("AllowMyOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Log Mongo settings safely
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var mongoOptions = app.Services.GetRequiredService<IOptions<MongoDBSettings>>().Value;

logger.LogInformation("MongoDBSettings configured. DatabaseName: {DatabaseName}", mongoOptions.DatabaseName);

// Pipeline
app.UseSwagger();
app.UseSwaggerUI();

if (!env.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AllowMyOrigins");

app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderStatusHub>("/api/OrderApi/orderStatusHub");

app.Run();

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (origins is { Length: > 0 })
    {
        return origins;
    }

    var rawOrigins = configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(rawOrigins))
    {
        return rawOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    throw new InvalidOperationException("Cors:AllowedOrigins is missing. Configure it in appsettings or env vars.");
}
