using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificationInvoicePrintTemplate",
                columns: table => new
                {
                    CertificationInvoicePrintTemplateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", unicode: false, maxLength: 250, nullable: true),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    EcfTypeId = table.Column<int>(type: "integer", nullable: false),
                    FileData = table.Column<byte[]>(type: "bytea", nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(500)", unicode: false, maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "application/pdf"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationInvoicePrintTemplate", x => x.CertificationInvoicePrintTemplateId);
                    table.ForeignKey(
                        name: "FK_CertificationInvoicePrintTemplate_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_CertificationInvoicePrintTemplate_EcfType",
                        column: x => x.EcfTypeId,
                        principalTable: "EcfType",
                        principalColumn: "EcfTypeId");
                });

            migrationBuilder.CreateTable(
                name: "CertificationStep",
                columns: table => new
                {
                    CertificationStepId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationStep", x => x.CertificationStepId);
                });

            migrationBuilder.CreateTable(
                name: "ENcf",
                columns: table => new
                {
                    ENcfId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NcfTypeId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENcf", x => x.ENcfId);
                    table.ForeignKey(
                        name: "FK_ENcf_EcfType",
                        column: x => x.NcfTypeId,
                        principalTable: "EcfType",
                        principalColumn: "EcfTypeId");
                });

            migrationBuilder.CreateTable(
                name: "CertificationProcess",
                columns: table => new
                {
                    CertificationProcessId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStepId = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationProcess", x => x.CertificationProcessId);
                    table.ForeignKey(
                        name: "FK_CertificationProcess_CertificationStep",
                        column: x => x.CurrentStepId,
                        principalTable: "CertificationStep",
                        principalColumn: "CertificationStepId");
                    table.ForeignKey(
                        name: "FK_CertificationProcess_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                });

            migrationBuilder.CreateTable(
                name: "CertificationDocument",
                columns: table => new
                {
                    CertificationDocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificationProcessId = table.Column<int>(type: "integer", nullable: false),
                    ENcfSecuence = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    ENcfId = table.Column<int>(type: "integer", nullable: false),
                    EcfTypeId = table.Column<int>(type: "integer", nullable: false),
                    XmlSent = table.Column<string>(type: "text", unicode: false, nullable: false),
                    XmlResponse = table.Column<string>(type: "text", unicode: false, nullable: true),
                    TrackId = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationDocument", x => x.CertificationDocumentId);
                    table.ForeignKey(
                        name: "FK_CertificationDocument_CertificationProcess",
                        column: x => x.CertificationProcessId,
                        principalTable: "CertificationProcess",
                        principalColumn: "CertificationProcessId");
                    table.ForeignKey(
                        name: "FK_CertificationDocument_ENcf",
                        column: x => x.ENcfId,
                        principalTable: "ENcf",
                        principalColumn: "ENcfId");
                    table.ForeignKey(
                        name: "FK_CertificationDocument_EcfType",
                        column: x => x.EcfTypeId,
                        principalTable: "EcfType",
                        principalColumn: "EcfTypeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificationDocument_CertificationProcessId",
                table: "CertificationDocument",
                column: "CertificationProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationDocument_EcfTypeId",
                table: "CertificationDocument",
                column: "EcfTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationDocument_ENcfId",
                table: "CertificationDocument",
                column: "ENcfId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationInvoicePrintTemplate_ClientId",
                table: "CertificationInvoicePrintTemplate",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationInvoicePrintTemplate_EcfTypeId",
                table: "CertificationInvoicePrintTemplate",
                column: "EcfTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationProcess_ClientId",
                table: "CertificationProcess",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationProcess_CurrentStepId",
                table: "CertificationProcess",
                column: "CurrentStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ENcf_NcfTypeId",
                table: "ENcf",
                column: "NcfTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationDocument");

            migrationBuilder.DropTable(
                name: "CertificationInvoicePrintTemplate");

            migrationBuilder.DropTable(
                name: "CertificationProcess");

            migrationBuilder.DropTable(
                name: "ENcf");

            migrationBuilder.DropTable(
                name: "CertificationStep");
        }
    }
}
