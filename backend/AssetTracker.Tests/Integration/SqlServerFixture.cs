using AssetTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace AssetTracker.Tests.Integration;

public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AssetTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetTrackerDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AssetTrackerDbContext(options);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SqlServerFixture>
{
}
