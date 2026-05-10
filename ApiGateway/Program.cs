using ApiGateway.Configuration;
using ApiGateway.Features.Checkout;
using ApiGateway.Features.Orders;
using ApiGateway.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowWebUI");
app.UseResponseCompression();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTracking();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => { options.DarkMode = false; });
}

app.UseHttpsRedirection();

app.MapCheckoutEndpoints();
app.MapOrderEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapReverseProxy();

app.Run();