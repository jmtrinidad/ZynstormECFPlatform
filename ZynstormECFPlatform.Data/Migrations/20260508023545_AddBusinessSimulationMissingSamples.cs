using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSimulationMissingSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                columns: new[] { "BusinessTypeId", "EcfType", "GuidId", "JsonData" },
                values: new object[] { 1, "31", "98765432-1234-5678-90ab-cdef12345601", "{\"IssuerRnc\":\"133009889\",\"IssuerName\":\"Transporte NJ, SRL\",\"IssuerAddress\":\"Ensanche Gregorio Luperon, Santiago\",\"CustomerRnc\":\"102620717\",\"CustomerName\":\"Morteros de Europa\",\"Items\":[{\"Name\":\"Servicio de Transporte de Carga\",\"Quantity\":1,\"UnitPrice\":6000.00,\"TaxPercentage\":0,\"BillingIndicator\":4}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 2, "98765432-1234-5678-90ab-cdef12345602", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Farmacia Salud\",\"IssuerAddress\":\"Av. 27 de Febrero 123\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Amoxicilina 500mg\",\"Quantity\":1,\"UnitPrice\":450.00,\"TaxPercentage\":0},{\"Name\":\"Vitamina C 1000mg\",\"Quantity\":2,\"UnitPrice\":300.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 3, "98765432-1234-5678-90ab-cdef12345603", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Repuestos El Motor\",\"IssuerAddress\":\"Calle Duarte 45\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Filtro de Aceite\",\"Quantity\":1,\"UnitPrice\":650.00,\"TaxPercentage\":18},{\"Name\":\"Aceite Sintético 5W30\",\"Quantity\":4,\"UnitPrice\":850.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 4, "98765432-1234-5678-90ab-cdef12345604", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Taller Los Amigos\",\"IssuerAddress\":\"Av. Imbert 78\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cambio de Aceite (Labor)\",\"Quantity\":1,\"UnitPrice\":1500.00,\"TaxPercentage\":18},{\"Name\":\"Revisión de Frenos\",\"Quantity\":1,\"UnitPrice\":800.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 5, "98765432-1234-5678-90ab-cdef12345605", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Surtidora El Pueblo\",\"IssuerAddress\":\"Calle Central 10\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Refresco 2L\",\"Quantity\":12,\"UnitPrice\":75.00,\"TaxPercentage\":18},{\"Name\":\"Arroz 10lb\",\"Quantity\":5,\"UnitPrice\":350.00,\"TaxPercentage\":0}]}" });

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "EcfType", "GuidId", "JsonData", "LastUpdateUtc", "RegisteredAt" },
                values: new object[,]
                {
                    { 6, 6, null, "32", "98765432-1234-5678-90ab-cdef12345606", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Librería Minerva\",\"IssuerAddress\":\"Calle Independencia 55\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cuaderno A4\",\"Quantity\":5,\"UnitPrice\":120.00,\"TaxPercentage\":18},{\"Name\":\"Lápiz de Grafito\",\"Quantity\":20,\"UnitPrice\":15.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 7, 7, null, "32", "98765432-1234-5678-90ab-cdef12345671", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Pinturas del Este\",\"IssuerAddress\":\"Av. Independencia 456\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cubeta Pintura Blanca Satinada\",\"Quantity\":1,\"UnitPrice\":2000.00,\"TaxPercentage\":18},{\"Name\":\"Brocha 4 Pulgadas Profesional\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 8, 8, null, "32", "98765432-1234-5678-90ab-cdef12345672", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Boutique Elegance\",\"IssuerAddress\":\"Calle del Sol 789\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Vestido de Gala Azul\",\"Quantity\":1,\"UnitPrice\":3500.00,\"TaxPercentage\":18},{\"Name\":\"Cinturón Cuero Genuino\",\"Quantity\":1,\"UnitPrice\":1000.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 9, 9, null, "32", "98765432-1234-5678-90ab-cdef12345673", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Colchones Confort\",\"IssuerAddress\":\"Av. Winston Churchill 101\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Colchón King Size Ortopédico\",\"Quantity\":1,\"UnitPrice\":12000.00,\"TaxPercentage\":18},{\"Name\":\"Almohada Memory Foam\",\"Quantity\":2,\"UnitPrice\":1500.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 10, 10, null, "32", "98765432-1234-5678-90ab-cdef12345674", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Restaurante Sabores\",\"IssuerAddress\":\"Calle Gourmet 202\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cena Especial del Chef (Dúo)\",\"Quantity\":1,\"UnitPrice\":2500.00,\"TaxPercentage\":18},{\"Name\":\"Botella de Vino Tinto Reserva\",\"Quantity\":1,\"UnitPrice\":700.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 11, 11, null, "32", "98765432-1234-5678-90ab-cdef12345675", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Café Aroma\",\"IssuerAddress\":\"Plaza Central Local 5\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Café Latte Grande\",\"Quantity\":2,\"UnitPrice\":175.00,\"TaxPercentage\":18},{\"Name\":\"Croissant de Almendras\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 11);

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                columns: new[] { "BusinessTypeId", "EcfType", "GuidId", "JsonData" },
                values: new object[] { 7, "32", "98765432-1234-5678-90ab-cdef12345671", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Pinturas del Este\",\"IssuerAddress\":\"Av. Independencia 456\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cubeta Pintura Blanca Satinada\",\"Quantity\":1,\"UnitPrice\":2000.00,\"TaxPercentage\":18},{\"Name\":\"Brocha 4 Pulgadas Profesional\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 8, "98765432-1234-5678-90ab-cdef12345672", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Boutique Elegance\",\"IssuerAddress\":\"Calle del Sol 789\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Vestido de Gala Azul\",\"Quantity\":1,\"UnitPrice\":3500.00,\"TaxPercentage\":18},{\"Name\":\"Cinturón Cuero Genuino\",\"Quantity\":1,\"UnitPrice\":1000.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 9, "98765432-1234-5678-90ab-cdef12345673", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Colchones Confort\",\"IssuerAddress\":\"Av. Winston Churchill 101\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Colchón King Size Ortopédico\",\"Quantity\":1,\"UnitPrice\":12000.00,\"TaxPercentage\":18},{\"Name\":\"Almohada Memory Foam\",\"Quantity\":2,\"UnitPrice\":1500.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 10, "98765432-1234-5678-90ab-cdef12345674", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Restaurante Sabores\",\"IssuerAddress\":\"Calle Gourmet 202\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cena Especial del Chef (Dúo)\",\"Quantity\":1,\"UnitPrice\":2500.00,\"TaxPercentage\":18},{\"Name\":\"Botella de Vino Tinto Reserva\",\"Quantity\":1,\"UnitPrice\":700.00,\"TaxPercentage\":18}]}" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                columns: new[] { "BusinessTypeId", "GuidId", "JsonData" },
                values: new object[] { 11, "98765432-1234-5678-90ab-cdef12345675", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Café Aroma\",\"IssuerAddress\":\"Plaza Central Local 5\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Café Latte Grande\",\"Quantity\":2,\"UnitPrice\":175.00,\"TaxPercentage\":18},{\"Name\":\"Croissant de Almendras\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}" });
        }
    }
}
