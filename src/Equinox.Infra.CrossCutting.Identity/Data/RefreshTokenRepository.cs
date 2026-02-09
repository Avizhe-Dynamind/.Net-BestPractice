using Equinox.Infra.CrossCutting.Identity.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Equinox.Infra.CrossCutting.Identity.Data
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly EquinoxIdentityContext _context;

        public RefreshTokenRepository(EquinoxIdentityContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<RefreshToken> GetByJwtIdAsync(string jwtId)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.JwtId == jwtId);
        }

        public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId
                    && !rt.IsRevoked
                    && !rt.IsUsed
                    && rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokeTokenAsync(string token)
        {
            var refreshToken = await GetByTokenAsync(token);
            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await UpdateAsync(refreshToken);
            }
        }

        public async Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow.AddDays(-30)) // Keep for 30 days for audit
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }
}
