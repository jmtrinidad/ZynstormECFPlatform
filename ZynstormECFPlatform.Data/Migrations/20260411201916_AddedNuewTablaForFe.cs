using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNuewTablaForFe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDgiiProduction",
                table: "Client",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IncomingEcfDocument",
                columns: table => new
                {
                    IncomingEcfDocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RncEmisor = table.Column<string>(type: "character varying(25)", unicode: false, maxLength: 25, nullable: false),
                    ENcf = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    TrackId = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RawXml = table.Column<string>(type: "text", nullable: false),
                    IsCommerciallyApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingEcfDocument", x => x.IncomingEcfDocumentId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncomingEcfDocument");

            migrationBuilder.DropColumn(
                name: "IsDgiiProduction",
                table: "Client");
        }
    }
}
