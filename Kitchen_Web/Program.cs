
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Restaurant_API.Services.OutletSer;

var builder = WebApplication.CreateBuilder(args);

WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Services.AddControllersWithViews(); // This line is crucial
// Inside ConfigureKestrel metho
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP connections on port 80
    // Consider configuring HTTPS options if you're running in
});
// Data Protection Keys Configuration
var dataProtectionKeysPath = "/root/.aspnet/DataProtection-Keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("UniqueApplicationNameAcrossAllInstances");

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
/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));*/


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", builder =>
    {
        builder.WithOrigins(
                 "https://restosolutionssaas.com" // The second web application origin
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Allows cookies, authorization headers with HTTPS
    });
});
var app = builder.Build();

builder.Configuration.AddJsonFile("Kitchen_Web_appsettings.json", optional: true, reloadOnChange: true);
// Configure the HTTP request pipeline.

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

app.UseCors("AllowMyOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
