using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccountPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingAccountMonthlyUsageId",
                table: "ClientPaymentItem",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "ClientPayment",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "BillingAccountId",
                table: "ClientPayment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentItem_AccountOverage_Usage",
                table: "ClientPaymentItem",
                column: "BillingAccountMonthlyUsageId",
                unique: true,
                filter: "\"ItemType\" = 2 AND \"BillingAccountMonthlyUsageId\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPayment_BillingAccountId_PaymentDate",
                table: "ClientPayment",
                columns: new[] { "BillingAccountId", "PaymentDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClientPayment_ClientOrAccount",
                table: "ClientPayment",
                sql: "(\"ClientId\" IS NULL) <> (\"BillingAccountId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientPayment_BillingAccount",
                table: "ClientPayment",
                column: "BillingAccountId",
                principalTable: "BillingAccount",
                principalColumn: "BillingAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientPaymentItem_BillingAccountMonthlyUsage",
                table: "ClientPaymentItem",
                column: "BillingAccountMonthlyUsageId",
                principalTable: "BillingAccountMonthlyUsage",
                principalColumn: "BillingAccountMonthlyUsageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientPayment_BillingAccount",
                table: "ClientPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientPaymentItem_BillingAccountMonthlyUsage",
                table: "ClientPaymentItem");

            migrationBuilder.DropIndex(
                name: "IX_ClientPaymentItem_AccountOverage_Usage",
                table: "ClientPaymentItem");

            migrationBuilder.DropIndex(
                name: "IX_ClientPayment_BillingAccountId_PaymentDate",
                table: "ClientPayment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClientPayment_ClientOrAccount",
                table: "ClientPayment");

            migrationBuilder.DropColumn(
                name: "BillingAccountMonthlyUsageId",
                table: "ClientPaymentItem");

            migrationBuilder.DropColumn(
                name: "BillingAccountId",
                table: "ClientPayment");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "ClientPayment",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
