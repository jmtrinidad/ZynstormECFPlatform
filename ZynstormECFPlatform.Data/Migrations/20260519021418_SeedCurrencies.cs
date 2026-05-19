using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Currency",
                columns: new[] { "CurrencyId", "Code", "DeletedTimeUtc", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 1, "DOP", null, null, "PESO DOMINICANO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "USD", null, null, "DOLAR ESTADOUNIDENSE", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "EUR", null, null, "EURO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, "BRL", null, null, "REAL BRASILENO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, "CAD", null, null, "DOLAR CANADIENSE", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, "CHF", null, null, "FRANCO SUIZO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, "CHY", null, null, "YUAN CHINO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, "XDR", null, null, "DERECHO ESPECIAL DE GIRO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, "DKK", null, null, "CORONA DANESA", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, "GBP", null, null, "LIBRA ESTERLINA", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 11, "JPY", null, null, "YEN JAPONES", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 12, "NOK", null, null, "CORONA NORUEGA", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 13, "SCP", null, null, "LIBRA ESCOCESA", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 14, "SEK", null, null, "CORONA SUECA", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 15, "VEF", null, null, "BOLIVAR FUERTE VENEZOLANO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 16, "HTG", null, null, "GURDA HAITIANA", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 17, "MXN", null, null, "PESO MEXICANO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 18, "COP", null, null, "PESO COLOMBIANO", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Currency",
                keyColumn: "CurrencyId",
                keyValue: 18);
        }
    }
}
