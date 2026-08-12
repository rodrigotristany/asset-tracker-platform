using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AssetTrackerDbContext _dbContext;

    public AdminUserRepository(AssetTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        return await _dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Username == username, ct);
    }
}
