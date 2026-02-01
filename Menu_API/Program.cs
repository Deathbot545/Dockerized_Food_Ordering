using Menu_API.Data;
using Menu_API.Services.MenuS;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// Load base + environment config (your custom naming scheme)
builder.Configuration
    .AddJsonFile("Menu_API_appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Menu_API_appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

// ---------------------------
// Hosting / Ports
// ---------------------------
// Local non-Docker dev: localhost:5005
// Docker/AWS: port 80
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (env.IsDevelopment())
        serverOptions.ListenLocalhost(5005);
    else
        serverOptions.ListenAnyIP(80);
});

// ---------------------------
// Services
// ---------------------------
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MenuDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MenuDbConnection")));

// ---------------------------
// CORS (env-aware)
// ---------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", policy =>
    {
        if (env.IsDevelopment())
        {
            policy.WithOrigins(
                    "http://localhost:5002", // Food_Ordering_Web
                    "http://localhost:5003", // Kitchen_Web (if you set it)
                    "http://localhost:5173"  // optional dev server
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://restosolutionssaas.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

var app = builder.Build();

// ---------------------------
// Logging
// ---------------------------
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var cs = builder.Configuration.GetConnectionString("MenuDbConnection");

if (env.IsDevelopment())
{
    logger.LogInformation("Using MenuDbConnection: {ConnectionString}", cs);
}
else
{
    logger.LogInformation("MenuDbConnection configured.");
}

// ---------------------------
// DB migrations
// ---------------------------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MenuDbContext>();

    try
    {
        logger.LogInformation("Applying migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
    }
}

// ---------------------------
// Pipeline
// ---------------------------
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors("AllowMyOrigins");

// Keep only if you use [Authorize] attributes; otherwise harmless but unnecessary.
app.UseAuthorization();

app.MapControllers();

app.Run();
