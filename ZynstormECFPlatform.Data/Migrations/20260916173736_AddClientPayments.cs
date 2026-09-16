using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientPayment",
                columns: table => new
                {
                    ClientPaymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", unicode: false, maxLength: 500, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RegisteredByUserId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPayment", x => x.ClientPaymentId);
                    table.ForeignKey(
                        name: "FK_ClientPayment_Client",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "ClientId");
                });

            migrationBuilder.CreateTable(
                name: "ClientPaymentItem",
                columns: table => new
                {
                    ClientPaymentItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientPaymentId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: true),
                    PlanName = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    MonthsCovered = table.Column<int>(type: "integer", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PreviousNextPaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NewNextPaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ClientMonthlyUsageId = table.Column<int>(type: "integer", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: true),
                    OverageDocuments = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GuidId = table.Column<string>(type: "character varying(450)", unicode: false, maxLength: 450, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LastUpdateUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPaymentItem", x => x.ClientPaymentItemId);
                    table.ForeignKey(
                        name: "FK_ClientPaymentItem_ClientMonthlyUsage",
                        column: x => x.ClientMonthlyUsageId,
                        principalTable: "ClientMonthlyUsage",
                        principalColumn: "ClientMonthlyUsageId");
                    table.ForeignKey(
                        name: "FK_ClientPaymentItem_ClientPayment",
                        column: x => x.ClientPaymentId,
                        principalTable: "ClientPayment",
                        principalColumn: "ClientPaymentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientPayment_ClientId_PaymentDate",
                table: "ClientPayment",
                columns: new[] { "ClientId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentItem_ClientPaymentId",
                table: "ClientPaymentItem",
                column: "ClientPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPaymentItem_Overage_Usage",
                table: "ClientPaymentItem",
                column: "ClientMonthlyUsageId",
                unique: true,
                filter: "\"ItemType\" = 2 AND \"ClientMonthlyUsageId\" IS NOT NULL AND NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientPaymentItem");

            migrationBuilder.DropTable(
                name: "ClientPayment");
        }
    }
}
