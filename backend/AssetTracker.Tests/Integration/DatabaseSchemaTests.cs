using Microsoft.Data.SqlClient;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class DatabaseSchemaTests
{
    private readonly SqlServerFixture _fixture;

    public DatabaseSchemaTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("devices")]
    [InlineData("locations")]
    [InlineData("admin_users")]
    public async Task Migration_CreatesExpectedTable(string tableName)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
        command.Parameters.AddWithValue("@TableName", tableName);

        var count = (int)(await command.ExecuteScalarAsync())!;

        Assert.Equal(1, count);
    }
}
