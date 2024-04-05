using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Food_Ordering_API.Models;
using Food_Ordering_API.Data;

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
//guyg

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