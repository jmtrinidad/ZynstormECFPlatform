using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class RiPrintTemplateRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificationInvoicePrintTemplate_EcfType",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.DropIndex(
                name: "IX_CertificationInvoicePrintTemplate_EcfTypeId",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.DropColumn(
                name: "EcfTypeId",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.AddColumn<string>(
                name: "ExtractionWarnings",
                table: "CertificationInvoicePrintTemplate",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LayoutJson",
                table: "CertificationInvoicePrintTemplate",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CertificationInvoicePrintTemplate",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CertificationInvoicePrintTemplateEcfTypes",
                columns: table => new
                {
                    CertificationInvoicePrintTemplateEcfTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificationInvoicePrintTemplateId = table.Column<int>(type: "integer", nullable: false),
                    EcfTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationInvoicePrintTemplateEcfTypes", x => x.CertificationInvoicePrintTemplateEcfTypeId);
                    table.ForeignKey(
                        name: "FK_CertificationInvoicePrintTemplateEcfTypes_CertificationInvo~",
                        column: x => x.CertificationInvoicePrintTemplateId,
                        principalTable: "CertificationInvoicePrintTemplate",
                        principalColumn: "CertificationInvoicePrintTemplateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificationInvoicePrintTemplateEcfTypes_EcfType_EcfTypeId",
                        column: x => x.EcfTypeId,
                        principalTable: "EcfType",
                        principalColumn: "EcfTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificationInvoicePrintTemplateEcfTypes_CertificationInvo~",
                table: "CertificationInvoicePrintTemplateEcfTypes",
                columns: new[] { "CertificationInvoicePrintTemplateId", "EcfTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificationInvoicePrintTemplateEcfTypes_EcfTypeId",
                table: "CertificationInvoicePrintTemplateEcfTypes",
                column: "EcfTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationInvoicePrintTemplateEcfTypes");

            migrationBuilder.DropColumn(
                name: "ExtractionWarnings",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.DropColumn(
                name: "LayoutJson",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CertificationInvoicePrintTemplate");

            migrationBuilder.AddColumn<int>(
                name: "EcfTypeId",
                table: "CertificationInvoicePrintTemplate",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "CertificationInvoicePrintTemplate",
                type: "character varying(500)",
                unicode: false,
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificationInvoicePrintTemplate_EcfTypeId",
                table: "CertificationInvoicePrintTemplate",
                column: "EcfTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationInvoicePrintTemplate_EcfType",
                table: "CertificationInvoicePrintTemplate",
                column: "EcfTypeId",
                principalTable: "EcfType",
                principalColumn: "EcfTypeId");
        }
    }
}
