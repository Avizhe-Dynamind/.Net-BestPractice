using Equinox.Infra.CrossCutting.Identity.Data;
using Equinox.Infra.CrossCutting.Identity.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Equinox.Services.Api.BackgroundServices
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAndSessionsAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during token cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Token Cleanup Service stopped");
        }

        private async Task CleanupExpiredTokensAndSessionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();

            var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

            try
            {
                _logger.LogInformation("Starting cleanup of expired tokens and sessions");

                await refreshTokenRepository.CleanupExpiredTokensAsync();
                await sessionService.CleanupExpiredSessionsAsync();

                _logger.LogInformation("Cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup process");
            }
        }
    }
}
