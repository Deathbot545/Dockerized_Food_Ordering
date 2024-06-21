using Microsoft.Extensions.Logging;
using Order_API.Data;
using Order_API.Hubs;
using Order_API.Service.Orderser;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Order_API_appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Configuration;

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80);
});

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowMyOrigins");

app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderStatusHub>("/api/OrderApi/orderStatusHub");

app.Run();

void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}
