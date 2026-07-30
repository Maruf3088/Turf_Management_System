using Microsoft.EntityFrameworkCore;
using turf_management_system.Data;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;

namespace turf_management_system.Repositories.Implementations
{
    public class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
    {
        public PasswordResetTokenRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task DeleteUserTokensAsync(int userId)
        {
            var tokens = await _dbSet.Where(t => t.UserId == userId).ToListAsync();
            if (tokens.Any())
            {
                _dbSet.RemoveRange(tokens);
            }
        }

        public async Task DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _dbSet.Where(t => t.ExpirationTime <= DateTime.UtcNow || t.IsUsed).ToListAsync();
            if (expiredTokens.Any())
            {
                _dbSet.RemoveRange(expiredTokens);
            }
        }
    }
}
