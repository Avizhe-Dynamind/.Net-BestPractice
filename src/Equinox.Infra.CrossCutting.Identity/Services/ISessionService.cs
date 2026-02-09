using Equinox.Infra.CrossCutting.Identity.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Services
{
    public interface ISessionService
    {
        Task<UserSession> CreateSessionAsync(string userId, string refreshToken, string ipAddress, string userAgent, string deviceId = null);
        Task<List<UserSession>> GetActiveSessionsAsync(string userId);
        Task UpdateSessionActivityAsync(string sessionToken);
        Task RevokeSessionAsync(Guid sessionId, string userId);
        Task RevokeAllSessionsAsync(string userId);
        Task CleanupExpiredSessionsAsync();
    }
}
