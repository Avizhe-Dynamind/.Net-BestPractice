using Equinox.Infra.CrossCutting.Identity.Configuration;
using Equinox.Services.Api.BackgroundServices;
using Equinox.Services.Api.Configurations;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("EQUINOX_")
    .AddUserSecrets<Program>(optional: true);

// Configure Services
builder.AddApiConfiguration()                   // Api Configurations
       .AddDatabaseConfiguration()              // Setting DBContexts
       .AddApiIdentityConfiguration()           // ASP.NET Identity Settings & JWT
       .AddSwaggerConfiguration()               // Swagger Config
       .AddCorsConfiguration()                  // CORS Config
       .AddRateLimitingConfiguration()          // Rate Limiting Config
       .AddDependencyInjectionConfiguration();  // DotNet Native DI Abstraction

// Register background service
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

// Configure
app.UseHttpsRedirection()
    .UseRateLimiter()
    .UseCors("DefaultPolicy")
    .UseAuthentication()
    .UseAuthorization();

app.MapControllers();
app.MapIdentityApi<IdentityUser>();

app.UseSwaggerSetup();
app.Run();