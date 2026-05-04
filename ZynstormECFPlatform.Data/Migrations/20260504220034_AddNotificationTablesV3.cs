using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTablesV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessLogs_AspNetUsers_UserId1",
                table: "UserAccessLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuditLogs_AspNetUsers_UserId1",
                table: "UserAuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAuditLogs",
                table: "UserAuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAccessLogs",
                table: "UserAccessLogs");

            migrationBuilder.RenameTable(
                name: "UserAuditLogs",
                newName: "UserAuditLog");

            migrationBuilder.RenameTable(
                name: "UserAccessLogs",
                newName: "UserAccessLog");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLogs_UserId1",
                table: "UserAuditLog",
                newName: "IX_UserAuditLog_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLogs_UserId",
                table: "UserAuditLog",
                newName: "IX_UserAuditLog_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAccessLogs_UserId1",
                table: "UserAccessLog",
                newName: "IX_UserAccessLog_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserAccessLogs_UserId",
                table: "UserAccessLog",
                newName: "IX_UserAccessLog_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAuditLog",
                table: "UserAuditLog",
                column: "UserAuditLogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAccessLog",
                table: "UserAccessLog",
                column: "UserAccessLogId");

            migrationBuilder.CreateTable(
                name: "NotificationType",
                columns: table => new
                {
                    NotificationTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationType", x => x.NotificationTypeId);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationConfiguration",
                columns: table => new
                {
                    UserNotificationConfigurationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    NotificationTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationConfiguration", x => x.UserNotificationConfigurationId);
                    table.ForeignKey(
                        name: "FK_UserNotificationConfiguration_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotificationConfiguration_NotificationType_Notification~",
                        column: x => x.NotificationTypeId,
                        principalTable: "NotificationType",
                        principalColumn: "NotificationTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NotificationType",
                columns: new[] { "NotificationTypeId", "DeletedTimeUtc", "Description", "GuidId", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 1, null, "Recibir email cuando una factura es aceptada por la DGII", "cb1d1ca4-8b40-4edd-ac56-3492e63a0019", null, "Factura Aceptada (Email)", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(2302) },
                    { 2, null, "Recibir email cuando una factura es rechazada por la DGII", "14aed387-1d9e-4a5d-8825-9fe72a568464", null, "Factura Rechazada (Email)", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(7365) },
                    { 3, null, "Recibir resumen diario de facturas procesadas", "7858f894-5c4c-4a9d-834f-8908cf889b97", null, "Reporte Diario", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(7399) },
                    { 4, null, "Recibir resumen semanal con estadísticas detalladas", "5f7e2ffe-bf2a-4b6e-9aac-d0d70f975d03", null, "Reporte Semanal", new DateTime(2026, 5, 4, 22, 0, 32, 658, DateTimeKind.Utc).AddTicks(7405) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationConfiguration_NotificationTypeId",
                table: "UserNotificationConfiguration",
                column: "NotificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationConfiguration_UserId",
                table: "UserNotificationConfiguration",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessLog_AspNetUsers_UserId1",
                table: "UserAccessLog",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuditLog_AspNetUsers_UserId1",
                table: "UserAuditLog",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessLog_AspNetUsers_UserId1",
                table: "UserAccessLog");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuditLog_AspNetUsers_UserId1",
                table: "UserAuditLog");

            migrationBuilder.DropTable(
                name: "UserNotificationConfiguration");

            migrationBuilder.DropTable(
                name: "NotificationType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAuditLog",
                table: "UserAuditLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAccessLog",
                table: "UserAccessLog");

            migrationBuilder.RenameTable(
                name: "UserAuditLog",
                newName: "UserAuditLogs");

            migrationBuilder.RenameTable(
                name: "UserAccessLog",
                newName: "UserAccessLogs");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLog_UserId1",
                table: "UserAuditLogs",
                newName: "IX_UserAuditLogs_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLog_UserId",
                table: "UserAuditLogs",
                newName: "IX_UserAuditLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAccessLog_UserId1",
                table: "UserAccessLogs",
                newName: "IX_UserAccessLogs_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_UserAccessLog_UserId",
                table: "UserAccessLogs",
                newName: "IX_UserAccessLogs_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "AspNetUsers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAuditLogs",
                table: "UserAuditLogs",
                column: "UserAuditLogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAccessLogs",
                table: "UserAccessLogs",
                column: "UserAccessLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessLogs_AspNetUsers_UserId1",
                table: "UserAccessLogs",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuditLogs_AspNetUsers_UserId1",
                table: "UserAuditLogs",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
