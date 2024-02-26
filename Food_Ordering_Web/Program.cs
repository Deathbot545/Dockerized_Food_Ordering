using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Microsoft.AspNetCore.CookiePolicy;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Constraints;
using Core.Services.MenuS;
using Core.Services.OutletSer;
using Core.Services.Orderser;
using Order_API.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Inside ConfigureKestrel method
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        try
        {
            listenOptions.UseHttps("/etc/ssl/certs/certificate.pfx", "raaed");
            logger.LogInformation("HTTPS setup was successful.");
        }
        catch (Exception ex)
        {
            // Log detailed information about the exception
            logger.LogError(ex, "An error occurred while setting up HTTPS.");
        }
    });
});

// Inside ConfigureServices method or directly in the Program.cs
var dataProtectionKeysPath = "/root/.aspnet/DataProtection-Keys"; // Path inside the container
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
// Register the HttpClient, set its base address, and configure the timeout
builder.Services.AddHttpClient("namedClient", c =>
{
    c.BaseAddress = new Uri(configuration["ApiBaseUrl"]);
    c.Timeout = TimeSpan.FromSeconds(200); // Set the desired timeout here
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services.AddSignalR();

builder.Services.AddScoped<IOutletService, OutletService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest; // Important for HTTP
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.Authority = "your-authority-here";  // e.g., https://your-auth-server.com/
        options.Audience = "your-audience-here";    // e.g., your-client-id
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Additional token validation parameters here
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Authentication:Google:ClientId"];
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
    });


// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

//SubDomain Stuff
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap.Add("subdomain", typeof(SubdomainRouteConstraint));
});

// Add controllers and Razor pages
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Add another HttpClient without a base address, if needed
builder.Services.AddHttpClient();

var app = builder.Build();

// Temporarily apply these for debugging purposes
app.UseDeveloperExceptionPage();


// Apply migrations automatically (consider the implications in production environments)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}


if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ShowDetailedErrors"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});


app.MapRazorPages();

app.Run();