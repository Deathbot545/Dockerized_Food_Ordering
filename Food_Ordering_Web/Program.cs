using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Inside ConfigureKestrel method
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

builder.Services.AddHttpClient("namedClient", c => { c.BaseAddress = new Uri(configuration["ApiBaseUrl"]); });

builder.Services.AddSignalR();

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP connections
    // Removed the ListenAnyIP(443) block that configures HTTPS
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
//builder.Services.AddScoped<AccountService, AccountService>();
builder.Services.AddSignalR();
//j

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

// Configure Authentication to include both JWT and Cookie authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Use cookies by default
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Use JWT for authentication
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Use JWT for challenges
})
.AddJwtBearer(options =>
{
    // Since you're issuing the tokens yourself, you define the parameters for validating those tokens here
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"], // Your application's issuer name
        ValidAudience = configuration["Jwt:Audience"], // Your application's audience name
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])), // The key used to sign the tokens
    };
})
.AddGoogle(options =>
{
    options.ClientId = configuration["Authentication:Google:ClientId"];
    options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
});

builder.Services.AddHttpClient("UserManagementApiClient", client =>
{
    client.BaseAddress = new Uri("https://your-user-management-api/");
    // Optionally set headers, timeouts, etc.
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

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ShowDetailedErrors"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

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