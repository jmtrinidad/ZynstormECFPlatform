using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToENcf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "ENcf",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ENcf_ClientId",
                table: "ENcf",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_ENcf_Client",
                table: "ENcf",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ENcf_Client",
                table: "ENcf");

            migrationBuilder.DropIndex(
                name: "IX_ENcf_ClientId",
                table: "ENcf");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ENcf");
        }
    }
}
