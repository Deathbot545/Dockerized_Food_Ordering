using Microsoft.Extensions.Logging;
using Order_API.Data;
using Order_API.Hubs;
using Order_API.Service.Orderser;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Order_API_appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Configuration;

var mongoSettings = configuration.GetSection(nameof(MongoDBSettings)).Get<MongoDBSettings>();
builder.Logging.AddConsole();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80);
});
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddSignalR();

builder.Services.AddControllers();

ConfigureSwagger(builder);

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

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MongoDBSettings ConnectionString: {ConnectionString}", mongoSettings.ConnectionString);
logger.LogInformation("MongoDBSettings DatabaseName: {DatabaseName}", mongoSettings.DatabaseName);

app.Run();

void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

