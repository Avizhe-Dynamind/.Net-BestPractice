using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace Equinox.Services.Api.Configurations
{
    public static class CorsConfig
    {
        public static WebApplicationBuilder AddCorsConfiguration(this WebApplicationBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var allowedOrigins = builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        // More permissive in development
                        policy.WithOrigins(allowedOrigins.Any() ? allowedOrigins : new[] { "http://localhost:3000", "https://localhost:3000" })
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                    else
                    {
                        // Strict in production
                        if (!allowedOrigins.Any())
                        {
                            throw new InvalidOperationException("AllowedOrigins must be configured for production");
                        }

                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()
                              .SetIsOriginAllowedToAllowWildcardSubdomains();
                    }
                });
            });

            return builder;
        }
    }
}
