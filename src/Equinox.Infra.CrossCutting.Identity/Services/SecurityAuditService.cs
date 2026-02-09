using Equinox.Infra.CrossCutting.Identity.Data;
using Equinox.Infra.CrossCutting.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Services
{
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly EquinoxIdentityContext _context;
        private readonly ILogger<SecurityAuditService> _logger;

        public SecurityAuditService(EquinoxIdentityContext context, ILogger<SecurityAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogEventAsync(
            SecurityEventType eventType,
            string userId,
            string userEmail,
            string ipAddress,
            string userAgent,
            bool isSuccessful,
            string description = null,
            object additionalData = null)
        {
            try
            {
                var auditLog = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserEmail = userEmail,
                    EventType = eventType,
                    EventDescription = description ?? eventType.ToString(),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Timestamp = DateTime.UtcNow,
                    IsSuccessful = isSuccessful,
                    AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
                };

                _context.SecurityAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Also log to standard logger for monitoring systems
                var logLevel = isSuccessful ? LogLevel.Information : LogLevel.Warning;
                _logger.Log(logLevel,
                    "Security Event: {EventType} for user {UserEmail} from {IpAddress} - Success: {IsSuccessful}",
                    eventType, userEmail, ipAddress, isSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event {EventType} for user {UserEmail}",
                    eventType, userEmail);
            }
        }

        public async Task<List<SecurityAuditLog>> GetUserSecurityLogsAsync(string userId, int take = 50)
        {
            return await _context.SecurityAuditLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<SecurityAuditLog>> GetFailedLoginAttemptsAsync(string ipAddress, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);

            return await _context.SecurityAuditLogs
                .Where(log => log.IpAddress == ipAddress
                    && log.EventType == SecurityEventType.LoginFailed
                    && log.Timestamp >= cutoffTime)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
    }
}
