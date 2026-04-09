using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEcfDocumentMissingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCountry",
                table: "EcfDocument",
                type: "character varying(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerForeignId",
                table: "EcfDocument",
                type: "character varying(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuerEmail",
                table: "EcfDocument",
                type: "character varying(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCustomerRnc",
                table: "EcfDocument",
                type: "character varying(25)",
                unicode: false,
                maxLength: 25,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerCountry",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "CustomerForeignId",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "IssuerEmail",
                table: "EcfDocument");

            migrationBuilder.DropColumn(
                name: "ReferenceCustomerRnc",
                table: "EcfDocument");
        }
    }
}
