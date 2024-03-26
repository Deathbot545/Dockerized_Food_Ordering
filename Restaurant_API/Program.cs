using Restaurant_API.Data;
using Microsoft.EntityFrameworkCore;
using Restaurant_API.Services.OutletSer;

var builder = WebApplication.CreateBuilder(args);


// Kestrel configuration for HTTPS
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("/etc/ssl/certs/certificate.pfx", "raaed");
    });
});

builder.Services.AddDbContext<OutletDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OutletDbConnection")));
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IOutletService, OutletService>();

/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));*/
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<OutletDbContext>();
    dbContext.Database.Migrate();
}
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