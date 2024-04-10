
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

app.Use(async (context, next) =>
{
    if (context.Request.Cookies.Any())
    {
        foreach (var cookie in context.Request.Cookies)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*"; // Ensure you adhere to CORS policies in production
            app.Logger.LogInformation($"Received cookie: {cookie.Key} = {cookie.Value}");
        }
    }
    else
    {
        app.Logger.LogInformation("No cookies received in this request.");
    }

    await next.Invoke();
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseEndpoints(endpoints =>
{

});

app.MapRazorPages();

app.Run();
