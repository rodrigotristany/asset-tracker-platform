using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "admin_users",
                columns: new[] { "id", "created_at", "password_hash", "username" },
                values: new object[] { 1, new DateTime(2026, 8, 12, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$w12C1tmcv4IC7YmfNIm9sOhwTrLehZMio3BmNDNKmrG/iDDu2RstC", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "admin_users",
                keyColumn: "id",
                keyValue: 1);
        }
    }
}
