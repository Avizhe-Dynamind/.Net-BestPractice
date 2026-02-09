using Microsoft.AspNetCore.Identity;
using System;

namespace Equinox.Infra.CrossCutting.Identity.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public string JwtId { get; set; } // Links to the access token
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // Session tracking
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string DeviceId { get; set; } // Optional: For device tracking

        // Navigation
        public virtual IdentityUser User { get; set; }
    }
}
