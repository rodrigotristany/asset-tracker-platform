using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct);
}
