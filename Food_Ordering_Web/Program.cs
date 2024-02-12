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

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Register the HttpClient and set its base address
builder.Services.AddHttpClient("namedClient", c => { c.BaseAddress = new Uri(configuration["ApiBaseUrl"]); });

builder.Services.AddSignalR();


builder.Services.AddScoped<IOutletService, OutletService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

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
// Apply migrations automatically (consider the implications in production environments)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // Add this line
app.UseAuthorization();   // Add this line

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<OrderStatusHub>("/orderStatusHub"); // Map your hub here

    endpoints.MapControllerRoute(
    name: "SubdomainRoute",
    pattern: "{controller=Home}/{action=SubdomainRedirection}/{id?}",
    constraints: new { subdomain = new SubdomainRouteConstraint() });


    // ... your other routes
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    endpoints.MapRazorPages();
});

app.MapRazorPages();

app.Run();
