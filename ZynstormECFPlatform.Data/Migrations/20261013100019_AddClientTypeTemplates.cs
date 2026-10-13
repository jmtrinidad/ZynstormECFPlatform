using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientTypeTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientTypeTemplates",
                columns: table => new
                {
                    ClientTypeTemplateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientTypeId = table.Column<int>(type: "integer", nullable: false),
                    RiHtmlTemplateId = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTypeTemplates", x => x.ClientTypeTemplateId);
                    table.ForeignKey(
                        name: "FK_ClientTypeTemplate_ClientType",
                        column: x => x.ClientTypeId,
                        principalTable: "ClientTypes",
                        principalColumn: "ClientTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientTypeTemplate_RiHtmlTemplate",
                        column: x => x.RiHtmlTemplateId,
                        principalTable: "RiHtmlTemplates",
                        principalColumn: "RiHtmlTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientTypeTemplates_ClientTypeId_RiHtmlTemplateId",
                table: "ClientTypeTemplates",
                columns: new[] { "ClientTypeId", "RiHtmlTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientTypeTemplates_RiHtmlTemplateId",
                table: "ClientTypeTemplates",
                column: "RiHtmlTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientTypeTemplates");
        }
    }
}
