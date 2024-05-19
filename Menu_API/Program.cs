using Menu_API.Data;
using Menu_API.Services.MenuS;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Menu_API_appsettings.json", optional: true, reloadOnChange: true);

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP 

});
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMenuService, MenuService>();
// Add services to the contain
builder.Services.AddControllers();

ConfigureSwagger(builder);
ConfigureControllers(builder);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", builder =>
    {
        builder.WithOrigins(
                 "https://restosolutionssaas.com" // The second web applic
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Allows cookies, authorization headers with HTTPS
    });
});
builder.Services.AddDbContext<MenuDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MenuDbConnection")));

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var connectionString = builder.Configuration.GetConnectionString("MenuDbConnection");
logger.LogInformation($"Using connection string: {connectionString}");

// Ensure Database is Created and Migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<MenuDbContext>();

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


// Ensure Database is Created and Migrations are Applied

//gf


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseRouting(); // This should be added to ensure UseCors works with routing

app.UseCors("AllowMyOrigins");
app.UseAuthorization();

app.MapControllers();

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
