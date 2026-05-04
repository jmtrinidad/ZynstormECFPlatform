using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserClientMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UseClient");

            migrationBuilder.CreateTable(
                name: "UserClient",
                columns: table => new
                {
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClient", x => new { x.UserId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_UserClient_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_UserClient_User",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserClient_ClientId",
                table: "UserClient",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserClient");

            migrationBuilder.CreateTable(
                name: "UseClient",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseClient", x => new { x.UserId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_UseClient_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_UseClient_User",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UseClient_ClientId",
                table: "UseClient",
                column: "ClientId");
        }
    }
}
