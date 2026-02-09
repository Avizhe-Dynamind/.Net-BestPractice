using Microsoft.AspNetCore.Identity;
using System;

namespace Equinox.Infra.CrossCutting.Identity.Models
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string SessionToken { get; set; } // Links to RefreshToken
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Location { get; set; } // Optional: City, Country
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public virtual IdentityUser User { get; set; }
    }
}
