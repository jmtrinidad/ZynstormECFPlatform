using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingAccountId",
                table: "Client",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuotaDocuments",
                table: "Client",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingAccount",
                columns: table => new
                {
                    BillingAccountId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CustomMonthlyFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    OfferStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastPaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NextPaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaidMonths = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PrepaymentDiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    PaymentGraceDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    FirstPaymentReminderSentFor = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinalPaymentReminderSentFor = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaymentSuspendedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAccount", x => x.BillingAccountId);
                    table.ForeignKey(
                        name: "FK_BillingAccount_Plan",
                        column: x => x.PlanId,
                        principalTable: "Plan",
                        principalColumn: "PlanId");
                    table.ForeignKey(
                        name: "FK_BillingAccount_Status",
                        column: x => x.StatusId,
                        principalTable: "Status",
                        principalColumn: "StatusId");
                    table.ForeignKey(
                        name: "FK_BillingAccount_User",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillingAccountMonthlyUsage",
                columns: table => new
                {
                    BillingAccountMonthlyUsageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingAccountId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    AcceptedDocuments = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PlanId = table.Column<int>(type: "integer", nullable: true),
                    PlanName = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    MonthlyFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyDocumentLimit = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAccountMonthlyUsage", x => x.BillingAccountMonthlyUsageId);
                    table.ForeignKey(
                        name: "FK_BillingAccountMonthlyUsage_BillingAccount",
                        column: x => x.BillingAccountId,
                        principalTable: "BillingAccount",
                        principalColumn: "BillingAccountId");
                    table.ForeignKey(
                        name: "FK_BillingAccountMonthlyUsage_Plan",
                        column: x => x.PlanId,
                        principalTable: "Plan",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_BillingAccountId",
                table: "Client",
                column: "BillingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccount_OwnerUserId",
                table: "BillingAccount",
                column: "OwnerUserId",
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccount_PlanId",
                table: "BillingAccount",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccount_StatusId",
                table: "BillingAccount",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccountMonthlyUsage_AccountId_Year_Month",
                table: "BillingAccountMonthlyUsage",
                columns: new[] { "BillingAccountId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccountMonthlyUsage_PlanId",
                table: "BillingAccountMonthlyUsage",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_BillingAccount",
                table: "Client",
                column: "BillingAccountId",
                principalTable: "BillingAccount",
                principalColumn: "BillingAccountId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_BillingAccount",
                table: "Client");

            migrationBuilder.DropTable(
                name: "BillingAccountMonthlyUsage");

            migrationBuilder.DropTable(
                name: "BillingAccount");

            migrationBuilder.DropIndex(
                name: "IX_Client_BillingAccountId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "BillingAccountId",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "QuotaDocuments",
                table: "Client");
        }
    }
}
