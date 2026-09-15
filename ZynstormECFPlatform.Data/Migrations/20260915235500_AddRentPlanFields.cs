using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRentPlanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Plan",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanTypeId",
                table: "Plan",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRentPaymentDate",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRentPaymentDate",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentDiscountPercent",
                table: "Client",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RentPaidFullYear",
                table: "Client",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Plan");

            migrationBuilder.DropColumn(
                name: "PlanTypeId",
                table: "Plan");

            migrationBuilder.DropColumn(
                name: "LastRentPaymentDate",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "NextRentPaymentDate",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RentDiscountPercent",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RentPaidFullYear",
                table: "Client");
        }
    }
}
