using Restaurant_API.Data;
using Microsoft.EntityFrameworkCore;
using Restaurant_API.Services.OutletSer;

var builder = WebApplication.CreateBuilder(args);


// Kestrel configuration for HTTPS
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP connections
    // Removed the ListenAnyIP(443) block that configures HTTPS
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IOutletService, OutletService>();

/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));*/
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

builder.Services.AddDbContext<OutletDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OutletDbConnection")));

var app = builder.Build();

// Ensure Database is Created and Migrations are Applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<OutletDbContext>();
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
//hfh

builder.Configuration.AddJsonFile("Restaurant_API_appsettings.json", optional: true, reloadOnChange: true);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // These should be outside of the IsDevelopment check if you want them available in production as well
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowMyOrigins");
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();