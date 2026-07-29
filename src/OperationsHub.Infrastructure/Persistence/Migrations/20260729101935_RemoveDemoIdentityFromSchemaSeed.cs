using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF migration operations use inline key and column arrays.
#pragma warning disable IDE0161 // Keep the EF-generated block-scoped migration namespace.

namespace OperationsHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDemoIdentityFromSchemaSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "REQUESTER", "10000000-0000-4000-8000-000000000001" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "TECHNICIAN", "10000000-0000-4000-8000-000000000002" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "MANAGER", "10000000-0000-4000-8000-000000000003" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "ADMINISTRATOR", "10000000-0000-4000-8000-000000000004" });

            DisableDemoUser(migrationBuilder, "10000000-0000-4000-8000-000000000001");
            DisableDemoUser(migrationBuilder, "10000000-0000-4000-8000-000000000002");
            DisableDemoUser(migrationBuilder, "10000000-0000-4000-8000-000000000003");
            DisableDemoUser(migrationBuilder, "10000000-0000-4000-8000-000000000004");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restoring accounts with a public demo password would reopen the issue this migration fixes.
            // Development startup recreates those accounts only in the Development environment.
        }

        private static void DisableDemoUser(MigrationBuilder migrationBuilder, string userId)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: userId,
                columns: new[] { "LockoutEnabled", "LockoutEnd", "PasswordHash", "SecurityStamp" },
                values: new object[] { true, new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero), null!, $"disabled-{userId}" });
        }
    }
}
