using turf_management_system.Models.Domain;

namespace turf_management_system.Repositories.Interfaces
{
    public interface IPasswordResetTokenRepository : IGenericRepository<PasswordResetToken>
    {
        Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
        Task DeleteUserTokensAsync(int userId);
        Task DeleteExpiredTokensAsync();
    }
}
