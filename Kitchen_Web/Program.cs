using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// Load base + environment config (your custom naming scheme)
builder.Configuration
    .AddJsonFile("Kitchen_Web_appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Kitchen_Web_appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Ports
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (env.IsDevelopment())
        serverOptions.ListenLocalhost(5003);
    else
        serverOptions.ListenAnyIP(80);
});

// Data Protection
if (env.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .SetApplicationName("UniqueApplicationNameAcrossAllInstances");
}
else
{
    var dataProtectionKeysPath = "/root/.aspnet/DataProtection-Keys";
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("UniqueApplicationNameAcrossAllInstances");
}

// HTTP Client
builder.Services.AddHttpClient("namedClient", c =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(apiBaseUrl))
        throw new InvalidOperationException("ApiBaseUrl missing in Kitchen_Web config.");

    c.BaseAddress = new Uri(apiBaseUrl);
    c.Timeout = TimeSpan.FromSeconds(200);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    if (env.IsDevelopment())
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }

    return new HttpClientHandler();
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = env.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
});

// Auth (JWT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("Jwt:Key missing in Kitchen_Web config.");

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

// CORS (env-aware)
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
            policy.WithOrigins("https://restosolutionssaas.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ShowDetailedErrors"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!env.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

if (!string.IsNullOrEmpty(builder.Configuration["PathBase"]))
{
    app.UsePathBase(builder.Configuration["PathBase"]);
}

app.UseRouting();
app.UseCors("AllowMyOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
