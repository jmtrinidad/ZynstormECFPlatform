using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSimulationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "UserAccessLog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "UserAccessLog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessTypes",
                columns: table => new
                {
                    BusinessTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTypes", x => x.BusinessTypeId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSimulationSamples",
                columns: table => new
                {
                    BusinessSimulationSampleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessTypeId = table.Column<int>(type: "integer", nullable: false),
                    EcfType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    JsonData = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSimulationSamples", x => x.BusinessSimulationSampleId);
                    table.ForeignKey(
                        name: "FK_BusinessSimulationSamples_BusinessTypes_BusinessTypeId",
                        column: x => x.BusinessTypeId,
                        principalTable: "BusinessTypes",
                        principalColumn: "BusinessTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 1,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "8353c320-168a-4d3e-b46b-64048f47aa06", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(3530) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 2,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "6a6402d7-72e0-4e0c-99d5-15ad06b31c46", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(7478) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 3,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "b5e7ece5-a1e0-4898-8031-6d1fde57089d", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(7504) });

            migrationBuilder.UpdateData(
                table: "NotificationType",
                keyColumn: "NotificationTypeId",
                keyValue: 4,
                columns: new[] { "GuidId", "RegisteredAt" },
                values: new object[] { "967a7834-7772-483f-a28e-afcc2403f6c5", new DateTime(2026, 5, 7, 1, 34, 50, 652, DateTimeKind.Utc).AddTicks(7508) });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSimulationSamples_BusinessTypeId",
                table: "BusinessSimulationSamples",
                column: "BusinessTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSimulationSamples");

            migrationBuilder.DropTable(
                name: "BusinessTypes");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "UserAccessLog",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "UserAccessLog",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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
    }
}
