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
    var allowedOrigins = GetAllowedOrigins(builder.Configuration);
    options.AddPolicy("AllowMyOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ---------------------------
// Logging
// ---------------------------
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MenuDbConnection configured.");

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

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (origins is { Length: > 0 })
    {
        return origins;
    }

    var rawOrigins = configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(rawOrigins))
    {
        return rawOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    throw new InvalidOperationException("Cors:AllowedOrigins is missing. Configure it in appsettings or env vars.");
}
