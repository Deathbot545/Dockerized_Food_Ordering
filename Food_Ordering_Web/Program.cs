using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using Food_Ordering_Web.Middleware;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

// OPTIONAL: support your custom naming scheme too (doesn't break anything if files don't exist)
builder.Configuration
    .AddJsonFile("Food_Ordering_Web_appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"Food_Ordering_Web_appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

var configuration = builder.Configuration;

// Hosting / Ports
var runningInContainer =
    string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true",
        StringComparison.OrdinalIgnoreCase);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Local VS dev: keep your localhost:5002
    if (env.IsDevelopment() && !runningInContainer)
        serverOptions.ListenLocalhost(5002);
    else
        serverOptions.ListenAnyIP(80); // Docker + Production
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
    var apiBaseUrl = configuration["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(apiBaseUrl))
        throw new InvalidOperationException("ApiBaseUrl is missing. Set it in configuration (appsettings/appsettings.Development or env vars).");

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

// Auth / Cookies (env-aware)
var isDev = env.IsDevelopment();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);

    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
    options.Cookie.SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None;

    options.SlidingExpiration = true;
})
.AddGoogle(options =>
{
    options.ClientId = configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? "";
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".AspNetCore.Cookies";

    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
    options.Cookie.SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None;

    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// MVC / Razor / JSON
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Error handling
if (app.Environment.IsDevelopment() || configuration.GetValue<bool>("ShowDetailedErrors"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Reverse proxy support
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Cookie Policy (env-aware)
app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = isDev ? CookieSecurePolicy.None : CookieSecurePolicy.Always
});

// Pipeline
if (!isDev)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<JwtTokenMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
