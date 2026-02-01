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
    options.AddPolicy("AllowMyOrigins", policy =>
    {
        if (env.IsDevelopment())
        {
            policy.WithOrigins(
                    "http://localhost:5002",
                    "http://localhost:5003",
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://restosolutionssaas.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Log Mongo settings safely
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var mongoOptions = app.Services.GetRequiredService<IOptions<MongoDBSettings>>().Value;

if (env.IsDevelopment())
    logger.LogInformation("MongoDBSettings ConnectionString: {ConnectionString}", mongoOptions.ConnectionString);
else
    logger.LogInformation("MongoDBSettings configured.");

logger.LogInformation("MongoDBSettings DatabaseName: {DatabaseName}", mongoOptions.DatabaseName);

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
