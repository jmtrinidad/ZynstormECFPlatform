using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientCustomMonthlyFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomMonthlyFee",
                table: "Client",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomMonthlyFee",
                table: "Client");
        }
    }
}
