using System.Data;
using AssetTracker.Application.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AssetTracker.Infrastructure.Repositories;

public class RetentionRepository : IRetentionRepository
{
    private readonly string _connectionString;

    public RetentionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> PurgeOldLocationsAsync(int retentionDays, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                "usp_Retention_PurgeOldLocations",
                new { RetentionDays = retentionDays },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}
