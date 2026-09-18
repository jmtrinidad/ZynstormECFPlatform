using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedSerials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneratedSerials",
                columns: table => new
                {
                    GeneratedSerialId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SerialHash = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    SerialSuffix = table.Column<string>(type: "character varying(5)", unicode: false, maxLength: 5, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedSerials", x => x.GeneratedSerialId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedSerials_SerialHash",
                table: "GeneratedSerials",
                column: "SerialHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedSerials");
        }
    }
}
