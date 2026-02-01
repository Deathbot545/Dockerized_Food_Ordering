using Food_Ordering_API.Data;
using Food_Ordering_API.Models;
using Food_Ordering_API.Services.AccountService;
using Food_Ordering_API.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// Load base + environment config (your custom naming scheme)
builder.Configuration
    .AddJsonFile("Food_Ordering_API_appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Food_Ordering_API_appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Hosting / Ports
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (env.IsDevelopment())
        serverOptions.ListenLocalhost(5000);
    else
        serverOptions.ListenAnyIP(80);
});

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<AccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddDbContext<ApplicationUserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationDbConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationUserDbContext>()
.AddDefaultTokenProviders();

// JWT Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("Jwt:Key is missing. Set it in configuration (custom appsettings files or env vars).");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", policy =>
    {
        if (env.IsDevelopment())
        {
            policy.WithOrigins(
                    "http://localhost:5002",
                    "http://localhost:5003",
                    "http://localhost:5173"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                    "https://restosolutionssaas.com:8443",
                    "https://restosolutionssaas.com"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Logging + migrations
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var cs = builder.Configuration.GetConnectionString("ApplicationDbConnection");
logger.LogInformation("ApplicationDbConnection configured.");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationUserDbContext>();
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

// Pipeline
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseCors("AllowMyOrigins");

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
