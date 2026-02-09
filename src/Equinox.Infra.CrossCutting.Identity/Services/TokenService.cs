using Equinox.Infra.CrossCutting.Identity.API;
using Equinox.Infra.CrossCutting.Identity.Data;
using Equinox.Infra.CrossCutting.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Services
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly AppJwtSettings _jwtSettings;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            UserManager<IdentityUser> userManager,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<AppJwtSettings> jwtSettings,
            ILogger<TokenService> logger)
        {
            _userManager = userManager;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<AuthenticationResult> GenerateTokensAsync(
            IdentityUser user,
            string ipAddress,
            string userAgent)
        {
            try
            {
                // Generate JWT access token
                var jwtId = Guid.NewGuid().ToString();
                var accessToken = GenerateJwtToken(user, jwtId);

                // Generate refresh token
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

                // Store refresh token in database
                var refreshTokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = refreshToken,
                    JwtId = jwtId,
                    IsUsed = false,
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshTokenExpiry,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

                _logger.LogInformation("Tokens generated for user {UserId} from {IpAddress}", user.Id, ipAddress);

                return new AuthenticationResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiration = DateTime.UtcNow.AddHours(_jwtSettings.Expiration),
                    RefreshTokenExpiration = refreshTokenExpiry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens for user {UserId}", user.Id);
                return new AuthenticationResult
                {
                    Success = false,
                    Errors = new List<string> { "Failed to generate authentication tokens" }
                };
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(
            string accessToken,
            string refreshToken,
            string ipAddress,
            string userAgent)
        {
            try
            {
                // Validate JWT structure (even if expired)
                var validatedToken = GetPrincipalFromToken(accessToken);
                if (validatedToken == null)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid access token" }
                    };
                }

                // Validate refresh token from database
                var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

                if (storedRefreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found: {RefreshToken}", refreshToken);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Refresh token does not exist" }
                    };
                }

                // Validation checks
                if (DateTime.UtcNow > storedRefreshToken.ExpiresAt)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Refresh token has expired" }
                    };
                }

                if (storedRefreshToken.IsRevoked)
                {
                    _logger.LogWarning("Revoked refresh token used: {RefreshToken} for user {UserId}",
                        refreshToken, storedRefreshToken.UserId);
                    // Potential token theft - revoke all tokens
                    await _refreshTokenRepository.RevokeAllUserTokensAsync(storedRefreshToken.UserId);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Refresh token has been revoked" }
                    };
                }

                if (storedRefreshToken.IsUsed)
                {
                    _logger.LogWarning("Used refresh token attempted: {RefreshToken} for user {UserId}",
                        refreshToken, storedRefreshToken.UserId);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Refresh token has already been used" }
                    };
                }

                // Validate JwtId matches
                var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (storedRefreshToken.JwtId != jti)
                {
                    _logger.LogWarning("JWT ID mismatch for refresh token {RefreshToken}", refreshToken);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Refresh token does not match JWT" }
                    };
                }

                // Mark current refresh token as used
                storedRefreshToken.IsUsed = true;
                await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

                // Generate new tokens
                var user = await _userManager.FindByIdAsync(storedRefreshToken.UserId);
                if (user == null)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "User not found" }
                    };
                }

                return await GenerateTokensAsync(user, ipAddress, userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new AuthenticationResult
                {
                    Success = false,
                    Errors = new List<string> { "Failed to refresh token" }
                };
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken, string userId)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (storedToken == null || storedToken.UserId != userId)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(storedToken);

            _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
            return true;
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
            _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
        }

        private string GenerateJwtToken(IdentityUser user, string jwtId)
        {
            return new JwtBuilder<IdentityUser, string>()
                .WithUserManager(_userManager)
                .WithJwtSettings(_jwtSettings)
                .WithEmail(user.Email)
                .WithJwtId(jwtId)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildToken();
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = false, // Don't validate expiry for refresh
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                       StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
