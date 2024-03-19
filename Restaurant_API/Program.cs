using Core.Services.AccountService;
using Core.Services.MenuS;
using Core.Services.OutletSer;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Kestrel configuration for HTTPS
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("/etc/ssl/certs/certificate.pfx", "raaed");
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IOutletService, OutletService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
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


var app = builder.Build();
builder.Configuration.AddJsonFile("Restaurant_API_appsettings.json", optional: true, reloadOnChange: true);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // These should be outside of the IsDevelopment check if you want them available in production as well
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowMyOrigins");
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();