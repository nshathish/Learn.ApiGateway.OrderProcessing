using Microsoft.EntityFrameworkCore;
using CartService.Features.Cart;
using CartService.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var productServiceUrl = builder.Configuration["ServiceUrls:ProductService"] ?? "https://localhost:5001";
builder.Services.AddHttpClient("ProductService", client => client.BaseAddress = new Uri(productServiceUrl));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    DbInitializer.Initialize(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => { options.DarkMode = false; });
}

app.UseHttpsRedirection();

app.MapCartEndpoints();

app.Run();