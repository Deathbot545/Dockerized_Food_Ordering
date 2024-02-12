using Core.Services.CartSter;
using Core.Services.MenuS;
using Core.Services.Orderser;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Order_API.Hubs;
using System;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyOrigins",
    builder =>
    {
        builder
               .SetIsOriginAllowed(origin => IsOriginAllowed(origin))
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

bool IsOriginAllowed(string origin)
{
    var allowedRegex = new Regex(@"^(https:\/\/)([\w-]+\.)*localhost:7257$");
    return allowedRegex.IsMatch(origin);
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

builder.Configuration.AddJsonFile("Order_API_appsettings.json", optional: true, reloadOnChange: true);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowMyOrigins");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHub<OrderStatusHub>("/orderStatusHub");

app.MapControllers();

app.Run();
