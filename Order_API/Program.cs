using Microsoft.Extensions.Logging;
using Order_API.Data;
using Order_API.Hubs;
using Order_API.Service.Orderser;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Configuration;

// Configure Kestrel to listen on port 80
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80);
});

// Register HTTP client and services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOrderService, OrderService>();

// Add SignalR
builder.Services.AddSignalR();

// Add Controllers
builder.Services.AddControllers();

// Configure Swagger
ConfigureSwagger(builder);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", builder =>
    {
        builder.WithOrigins("https://restosolutionssaas.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Configure MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection(nameof(MongoDBSettings)));

builder.Services.AddSingleton<MongoDBContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable Swagger in production
app.UseSwagger();
app.UseSwaggerUI();

// Enable HTTPS redirection
app.UseHttpsRedirection();

// Enable routing
app.UseRouting();

// Enable CORS
app.UseCors("AllowMyOrigins");

// Enable authorization
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<OrderStatusHub>("/api/OrderApi/orderStatusHub");

// Run the application
app.Run();

void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

