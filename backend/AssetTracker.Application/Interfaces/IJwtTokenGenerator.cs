using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(AdminUser user);
}
