using Equinox.Infra.CrossCutting.Identity.Data;
using Equinox.Infra.CrossCutting.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Services
{
    public class SessionService : ISessionService
    {
        private readonly EquinoxIdentityContext _context;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            EquinoxIdentityContext context,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<SessionService> logger)
        {
            _context = context;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        public async Task<UserSession> CreateSessionAsync(
            string userId,
            string refreshToken,
            string ipAddress,
            string userAgent,
            string deviceId = null)
        {
            var deviceName = ParseDeviceName(userAgent);

            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionToken = refreshToken,
                DeviceId = deviceId ?? GenerateDeviceId(userAgent, ipAddress),
                DeviceName = deviceName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session created for user {UserId} from {IpAddress} on {Device}",
                userId, ipAddress, deviceName);

            return session;
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        public async Task UpdateSessionActivityAsync(string sessionToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeSessionAsync(Guid sessionId, string userId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session != null)
            {
                session.IsActive = false;

                // Revoke associated refresh token
                await _refreshTokenRepository.RevokeTokenAsync(session.SessionToken);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Session {SessionId} revoked for user {UserId}", sessionId, userId);
            }
        }

        public async Task RevokeAllSessionsAsync(string userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
            await _context.SaveChangesAsync();

            _logger.LogInformation("All sessions revoked for user {UserId}", userId);
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            _context.UserSessions.RemoveRange(expiredSessions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }

        private string ParseDeviceName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown Device";

            if (userAgent.Contains("iPhone")) return "iPhone";
            if (userAgent.Contains("iPad")) return "iPad";
            if (userAgent.Contains("Android")) return "Android Device";
            if (userAgent.Contains("Windows")) return "Windows PC";
            if (userAgent.Contains("Macintosh")) return "Mac";
            if (userAgent.Contains("Linux")) return "Linux PC";

            return "Web Browser";
        }

        private string GenerateDeviceId(string userAgent, string ipAddress)
        {
            var combined = $"{userAgent}{ipAddress}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }
    }
}
