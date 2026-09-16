using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRentPaymentReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RentFinalReminderSentFor",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentFirstReminderSentFor",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RentPaymentGraceDays",
                table: "Client",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentSuspendedAtUtc",
                table: "Client",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RentFinalReminderSentFor",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RentFirstReminderSentFor",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RentPaymentGraceDays",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "RentSuspendedAtUtc",
                table: "Client");
        }
    }
}
