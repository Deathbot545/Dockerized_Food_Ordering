
using Microsoft.EntityFrameworkCore;
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

// Signa
builder.Services.AddSignalR();

builder.Services.AddControllers();

ConfigureSwagger(builder);
ConfigureControllers(builder);

// Modify your existing CORS policy setup in th
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


builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDbConnection")));

var app = builder.Build();

// Ensure Database is Created and Migrations are Applied gg
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<OrderDbContext>();
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            // This ensures that the database is created and all migrations are applied.
            dbContext.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        // Log errors or handle them as needed
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

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
app.MapHub<OrderStatusHub>("/api/OrderApi/orderStatusHub");

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
