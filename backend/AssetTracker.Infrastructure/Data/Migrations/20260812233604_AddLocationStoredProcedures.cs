using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "LocationTableType.sql")));
            migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_Insert.sql")));
            migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_BatchInsert.sql")));
            migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_GetLatestByDevice.sql")));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_GetLatestByDevice;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_BatchInsert;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_Insert;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS LocationTableType;");
        }
    }
}
