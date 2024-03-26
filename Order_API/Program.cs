
using Microsoft.EntityFrameworkCore;
using Order_API.Data;
using Order_API.Hubs;
using Order_API.Service.Orderser;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("/etc/ssl/certs/certificate.pfx", "raaed");
    });
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDbConnection")));

// Dependency Injection for services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOrderService, OrderService>();

// SignalR for real-time web functionality
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers();

ConfigureSwagger(builder);
ConfigureControllers(builder);

// CORS policy setup
// Modify your existing CORS policy setup in the Program.cs file
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", builder =>
    {
        builder.WithOrigins(
                 "https://restosolutionssaas.com:8443", // The first web application origin
                 "https://restosolutionssaas.com" // The second web application origin
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Allows cookies, authorization headers with HTTPS
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
}

builder.Configuration.AddJsonFile("Order_API_appsettings.json", optional: true, reloadOnChange: true);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // By default, this will serve the Swagger UI at /swagger
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowMyOrigins");


app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderStatusHub>("/orderStatusHub");

app.Run();

void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureControllers(WebApplicationBuilder builder)
{
    builder.Services.AddControllers();
}
