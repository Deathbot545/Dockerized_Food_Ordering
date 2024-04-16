
using Food_Ordering_API.Data;
using Food_Ordering_API.Models;
using Food_Ordering_API.Services.AccountService;
using Food_Ordering_API.Services.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration.AddJsonFile("Food_Ordering_API_appsettings.json", optional: true, reloadOnChange: true);


builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ListenAnyIP(80); // Listen for HTTP connections
    // Removed the ListenAnyIP(443) block that configures HTTPS
});
ConfigureIdentity(builder);
ConfigureSwagger(builder);
ConfigureControllers(builder);

// Add this line to add in-memory caching
builder.Services.AddDistributedMemoryCache();

// Register AccountService
builder.Services.AddScoped<AccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins", builder =>
    {
        builder.WithOrigins(
                 "https://restosolutionssaas.com:8443", // The first web application origin
                 "https://restosolutionssaas.com" // The second web application origin
               )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Allows cookies, authorization headers with HTTPS
    });
});

builder.Services.AddDbContext<ApplicationUserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationDbConnection")));

var app = builder.Build();

// Log the connection string to verify it's correctly loaded
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbConnection");
logger.LogInformation($"Using connection string: {connectionString}");

// Ensure Database is Created and Migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationUserDbContext>();

    try
    {
        logger.LogInformation("Applying migrations to ensure the database and tables are created...");
        dbContext.Database.Migrate(); // This will ensure DB exists and all migrations are applied.
        logger.LogInformation("Database check and migration applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
   
}

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowMyOrigins");

app.UseAuthentication(); // Important: place after UseRouting and before UseAuthorization
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();

void ConfigureIdentity(WebApplicationBuilder builder)
{
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Configure identity options as needed
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        // etc.
    })
    .AddEntityFrameworkStores<ApplicationUserDbContext>()
    .AddDefaultTokenProviders();

    // Continue to configure external authentication providers as before
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });
}
//hoho

void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureControllers(WebApplicationBuilder builder)
{
    builder.Services.AddControllers();
}
