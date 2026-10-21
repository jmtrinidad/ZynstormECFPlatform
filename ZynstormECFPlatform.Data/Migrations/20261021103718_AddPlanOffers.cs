using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferMonths",
                table: "Plan",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferPrice",
                table: "Plan",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfferMonthsCovered",
                table: "ClientPaymentItem",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferUnitPrice",
                table: "ClientPaymentItem",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferStartDate",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferMonths",
                table: "Plan");

            migrationBuilder.DropColumn(
                name: "OfferPrice",
                table: "Plan");

            migrationBuilder.DropColumn(
                name: "OfferMonthsCovered",
                table: "ClientPaymentItem");

            migrationBuilder.DropColumn(
                name: "OfferUnitPrice",
                table: "ClientPaymentItem");

            migrationBuilder.DropColumn(
                name: "OfferStartDate",
                table: "Client");
        }
    }
}
