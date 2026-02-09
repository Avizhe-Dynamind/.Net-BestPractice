using Equinox.Infra.CrossCutting.Identity.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Services
{
    public interface ISecurityAuditService
    {
        Task LogEventAsync(SecurityEventType eventType, string userId, string userEmail, string ipAddress, string userAgent, bool isSuccessful, string description = null, object additionalData = null);
        Task<List<SecurityAuditLog>> GetUserSecurityLogsAsync(string userId, int take = 50);
        Task<List<SecurityAuditLog>> GetFailedLoginAttemptsAsync(string ipAddress, TimeSpan timeWindow);
    }
}
