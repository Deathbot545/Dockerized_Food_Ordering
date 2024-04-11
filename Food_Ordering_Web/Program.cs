
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.VisualStudio.Services.Users;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Configure Kestrel
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP connections on port 80
    // Consider configuring HTTPS options if you're running in production
});
//kk
// Data Protection Keys Configuration
var dataProtectionKeysPath = "/root/.aspnet/DataProtection-Keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("UniqueApplicationNameAcrossAllInstances");

// HTTP Client Configuration
builder.Services.AddHttpClient("namedClient", c =>
{
    c.BaseAddress = new Uri(configuration["ApiBaseUrl"]);
    c.Timeout = TimeSpan.FromSeconds(200); // Example timeout
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Authentication Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
    };
})
.AddCookie(options => // This is where you set the cookie options for authentication cookies
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Always use Secure cookies
    options.Cookie.SameSite = SameSiteMode.None; // Adjust according to your cross-site request needs
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Set your desired expiration
})
.AddGoogle(options =>
{
    options.ClientId = configuration["Authentication:Google:ClientId"];
    options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
});

// Controllers and Razor Pages
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// Configure JSON Options if necessary
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Middleware Configuration
if (app.Environment.IsDevelopment() || configuration.GetValue<bool>("ShowDetailedErrors"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always
});
// Make sure this is above app.UseAuthentication() and app.UseAuthorization()


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.Use(async (context, next) =>
{
    // Before continuing to the next middleware
    await next();

    // Log claims after authentication
    if (context.User.Identity.IsAuthenticated)
    {
        app.Logger.LogInformation($"Authenticated User: {context.User.Identity.Name}");
        foreach (var claim in context.User.Claims)
        {
            app.Logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }
    }
    else
    {
        app.Logger.LogInformation("User is not authenticated.");
    }
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});


app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapRazorPages();

app.Run();
