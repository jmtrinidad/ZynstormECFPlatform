using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeClientBrancheIdOptionalInEcfDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ClientBrancheId",
                table: "EcfDocument",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ClientBrancheId",
                table: "EcfDocument",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
