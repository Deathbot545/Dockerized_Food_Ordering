using Restaurant_API.Data;
using Microsoft.EntityFrameworkCore;
using Restaurant_API.Services.OutletSer;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Restaurant_API_appsettings.json", optional: true, reloadOnChange: true);

// Kestrel configurat
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); 
});

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IOutletService, OutletService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", builder =>
    {
        builder.WithOrigins(
                 "https://restosolutionssaas.com:8443", // The first web appl
                 "https://restosolutionssaas.com" 
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Allows cookies, authorization headers with HTTPS
    });
});

builder.Services.AddDbContext<OutletDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OutletDbConnection")));

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var connectionString = builder.Configuration.GetConnectionString("OutletDbConnection");
logger.LogInformation($"Using connection string: {connectionString}");

// Ensure Database is Created and Migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<OutletDbContext>();

    try
    {
        logger.LogInformation("Applying migrations to ensure the database and tables are created...");
        dbContext.Database.Migrate(); // This will ensure DB exists and all migrations are applied.
        logger.LogInformation("Database check and migration applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
    }
}


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