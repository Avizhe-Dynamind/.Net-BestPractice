using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Services
{
    public interface ITokenService
    {
        Task<AuthenticationResult> GenerateTokensAsync(IdentityUser user, string ipAddress, string userAgent);
        Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken, string ipAddress, string userAgent);
        Task<bool> RevokeTokenAsync(string refreshToken, string userId);
        Task RevokeAllUserTokensAsync(string userId);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
