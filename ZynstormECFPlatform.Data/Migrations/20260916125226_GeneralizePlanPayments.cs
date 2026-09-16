using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <summary>
    /// Generaliza los datos de pago de renta a cualquier plan.
    /// Escrita a mano: el scaffolding emparejaba mal los renombres entre columnas de fecha del mismo tipo.
    /// RentPaidFullYear se convierte en PaidMonths (true = 12 meses) antes de eliminarse.
    /// </summary>
    public partial class GeneralizePlanPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "LastRentPaymentDate", table: "Client", newName: "LastPaymentDate");
            migrationBuilder.RenameColumn(name: "NextRentPaymentDate", table: "Client", newName: "NextPaymentDate");
            migrationBuilder.RenameColumn(name: "RentDiscountPercent", table: "Client", newName: "PrepaymentDiscountPercent");
            migrationBuilder.RenameColumn(name: "RentPaymentGraceDays", table: "Client", newName: "PaymentGraceDays");
            migrationBuilder.RenameColumn(name: "RentFirstReminderSentFor", table: "Client", newName: "FirstPaymentReminderSentFor");
            migrationBuilder.RenameColumn(name: "RentFinalReminderSentFor", table: "Client", newName: "FinalPaymentReminderSentFor");
            migrationBuilder.RenameColumn(name: "RentSuspendedAtUtc", table: "Client", newName: "PaymentSuspendedAtUtc");

            migrationBuilder.AddColumn<int>(
                name: "PaidMonths",
                table: "Client",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE \"Client\" SET \"PaidMonths\" = 12 WHERE \"RentPaidFullYear\" = TRUE;");

            migrationBuilder.DropColumn(name: "RentPaidFullYear", table: "Client");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RentPaidFullYear",
                table: "Client",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Client\" SET \"RentPaidFullYear\" = TRUE WHERE \"PaidMonths\" >= 12;");

            migrationBuilder.DropColumn(name: "PaidMonths", table: "Client");

            migrationBuilder.RenameColumn(name: "LastPaymentDate", table: "Client", newName: "LastRentPaymentDate");
            migrationBuilder.RenameColumn(name: "NextPaymentDate", table: "Client", newName: "NextRentPaymentDate");
            migrationBuilder.RenameColumn(name: "PrepaymentDiscountPercent", table: "Client", newName: "RentDiscountPercent");
            migrationBuilder.RenameColumn(name: "PaymentGraceDays", table: "Client", newName: "RentPaymentGraceDays");
            migrationBuilder.RenameColumn(name: "FirstPaymentReminderSentFor", table: "Client", newName: "RentFirstReminderSentFor");
            migrationBuilder.RenameColumn(name: "FinalPaymentReminderSentFor", table: "Client", newName: "RentFinalReminderSentFor");
            migrationBuilder.RenameColumn(name: "PaymentSuspendedAtUtc", table: "Client", newName: "RentSuspendedAtUtc");
        }
    }
}
