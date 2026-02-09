using Equinox.Infra.CrossCutting.Identity.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Data
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task<RefreshToken> GetByJwtIdAsync(string jwtId);
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task RevokeAllUserTokensAsync(string userId);
        Task RevokeTokenAsync(string token);
        Task CleanupExpiredTokensAsync();
    }
}
