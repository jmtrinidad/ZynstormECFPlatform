using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPageAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomePagePath",
                table: "AspNetUsers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PageAccessConfigured",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserPageAccess",
                columns: table => new
                {
                    UserPageAccessId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PagePath = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPageAccess", x => x.UserPageAccessId);
                    table.ForeignKey(
                        name: "FK_UserPageAccess_User",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPageAccess_UserId_PagePath",
                table: "UserPageAccess",
                columns: new[] { "UserId", "PagePath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPageAccess");

            migrationBuilder.DropColumn(
                name: "HomePagePath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PageAccessConfigured",
                table: "AspNetUsers");
        }
    }
}
