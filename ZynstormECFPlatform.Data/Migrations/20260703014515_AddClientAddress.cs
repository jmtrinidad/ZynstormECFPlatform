using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Client",
                type: "character varying(300)",
                unicode: false,
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Client");
        }
    }
}
