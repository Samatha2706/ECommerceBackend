using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[] { 17, new DateTime(2026, 9, 2, 4, 24, 51, 671, DateTimeKind.Utc).AddTicks(7784), "somidiAdmin@gmail.com", "System Administrator", "$2a$11$dHvhpXMc1FlBXhifuzTWWu9e9eF3uHqbmkDrkW1h4HCDinIK/L4CO", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 17);
        }
    }
}
