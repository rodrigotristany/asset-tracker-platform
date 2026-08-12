namespace AssetTracker.Application.Interfaces;

public interface IRetentionRepository
{
    Task<int> PurgeOldLocationsAsync(int retentionDays, CancellationToken ct);
}
