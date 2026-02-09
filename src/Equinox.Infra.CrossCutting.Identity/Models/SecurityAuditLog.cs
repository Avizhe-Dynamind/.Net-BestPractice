using System;

namespace Equinox.Infra.CrossCutting.Identity.Models
{
    public enum SecurityEventType
    {
        LoginSuccess,
        LoginFailed,
        LoginLockedOut,
        Logout,
        LogoutAll,
        TokenRefreshed,
        TokenRefreshFailed,
        SessionRevoked,
        AllSessionsRevoked,
        PasswordChanged,
        AccountCreated,
        AccountDeleted,
        UnauthorizedAccess,
        SuspiciousActivity
    }

    public class SecurityAuditLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public SecurityEventType EventType { get; set; }
        public string EventDescription { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccessful { get; set; }
        public string AdditionalData { get; set; } // JSON string for extra context
    }
}
