using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRiHtmlFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientTypeId",
                table: "Client",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiHtmlTemplateId",
                table: "Client",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RiHtmlTemplates",
                columns: table => new
                {
                    RiHtmlTemplateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Html = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiHtmlTemplates", x => x.RiHtmlTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "ClientTypes",
                columns: table => new
                {
                    ClientTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RiHtmlTemplateId = table.Column<int>(type: "integer", nullable: true),
                    SampleDataJson = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTypes", x => x.ClientTypeId);
                    table.ForeignKey(
                        name: "FK_ClientType_RiHtmlTemplate",
                        column: x => x.RiHtmlTemplateId,
                        principalTable: "RiHtmlTemplates",
                        principalColumn: "RiHtmlTemplateId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_ClientTypeId",
                table: "Client",
                column: "ClientTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Client_RiHtmlTemplateId",
                table: "Client",
                column: "RiHtmlTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTypes_Name",
                table: "ClientTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientTypes_RiHtmlTemplateId",
                table: "ClientTypes",
                column: "RiHtmlTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_ClientType",
                table: "Client",
                column: "ClientTypeId",
                principalTable: "ClientTypes",
                principalColumn: "ClientTypeId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Client_RiHtmlTemplate",
                table: "Client",
                column: "RiHtmlTemplateId",
                principalTable: "RiHtmlTemplates",
                principalColumn: "RiHtmlTemplateId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_ClientType",
                table: "Client");

            migrationBuilder.DropForeignKey(
                name: "FK_Client_RiHtmlTemplate",
                table: "Client");

            migrationBuilder.DropTable(
                name: "ClientTypes");

            migrationBuilder.DropTable(
                name: "RiHtmlTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Client_ClientTypeId",
                table: "Client");

            migrationBuilder.DropIndex(
                name: "IX_Client_RiHtmlTemplateId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ClientTypeId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RiHtmlTemplateId",
                table: "Client");
        }
    }
}
