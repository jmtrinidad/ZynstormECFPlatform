using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIsCertified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCertified",
                table: "Client",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 1,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "ed5cf289-a470-4a0b-bc41-fea04b8f1d32", new DateTime(2026, 5, 4, 22, 59, 6, 71, DateTimeKind.Utc).AddTicks(9929) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 2,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "dc39f2cd-a6d2-4aa9-b210-daf55fbd157a", new DateTime(2026, 5, 4, 22, 59, 6, 73, DateTimeKind.Utc).AddTicks(1446) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 3,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "70e4b4d0-a8bb-475c-952c-3a2a66ba5962", new DateTime(2026, 5, 4, 22, 59, 6, 73, DateTimeKind.Utc).AddTicks(1572) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 4,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "61843d6d-5c11-431c-bad1-059f54b5f289", new DateTime(2026, 5, 4, 22, 59, 6, 73, DateTimeKind.Utc).AddTicks(1577) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCertified",
                table: "Client");

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 1,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "cb1d1ca4-8b40-4edd-ac56-3492e63a0019", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(2302) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 2,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "14aed387-1d9e-4a5d-8825-9fe72a568464", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(7365) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 3,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "7858f894-5c4c-4a9d-834f-8908cf889b97", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(7399) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 4,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "5f7e2ffe-bf2a-4b6e-9aac-d0d70f975d03", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(7405) });
        }
    }
}
