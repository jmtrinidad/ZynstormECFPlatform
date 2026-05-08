using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewBusinessTypesAndSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessType",
                columns: new[] { "BusinessTypeId", "DeletedTimeUtc", "Description", "GuidId", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 12, null, "Venta de equipos para el hogar y dispositivos electrónicos.", "8a9b0c1d-1e2f-3g4h-5i6j-8k9l0m1n2o3q", null, "Tienda de electrodomésticos", new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 13, null, "Venta de muebles, decoración y artículos para el hogar.", "9b0c1d2e-3f4g-5h6i-7j8k-9l0m1n2o3p4r", null, "Mueblería", new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Local) }
                });

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "Description", "EcfType", "GuidId", "JsonData", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 21, 12, null, "Ejemplo validado para Tipo 31.", "31", "98765432-1234-5678-90ab-cdef12345621", "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"NEVERA SAMSUNG BESPOKE 23 P3\",\"quantity\":1,\"unitPrice\":85000.00,\"billingIndicator\":1},{\"name\":\"TELEVISOR LG OLED 55\\\"\",\"quantity\":1,\"unitPrice\":65000.00,\"billingIndicator\":1}]}", null, "Tienda Electrodomésticos Crédito Fiscal", new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 22, 12, null, "Ejemplo validado para Tipo 32.", "32", "98765432-1234-5678-90ab-cdef12345622", "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LICUADORA NINJA PROFESSIONAL\",\"quantity\":1,\"unitPrice\":8500.00,\"billingIndicator\":1},{\"name\":\"FREIDORA DE AIRE DIGITAL 5.5L\",\"quantity\":1,\"unitPrice\":7200.00,\"billingIndicator\":1}]}", null, "Tienda Electrodomésticos Consumo", new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 23, 13, null, "Ejemplo validado para Tipo 31.", "31", "98765432-1234-5678-90ab-cdef12345623", "{\"ncf\":\"E310000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"MUEBLES Y DECORACIONES S.A.\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"JUEGO DE COMEDOR MODERNO 6 SILLAS\",\"quantity\":1,\"unitPrice\":45000.00,\"billingIndicator\":1}]}", null, "Mueblería Crédito Fiscal", new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 24, 13, null, "Ejemplo validado para Tipo 32.", "32", "98765432-1234-5678-90ab-cdef12345624", "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SOFA SECCIONAL EN TELA GRIS\",\"quantity\":1,\"unitPrice\":38000.00,\"billingIndicator\":1},{\"name\":\"CAMA QUEEN SIZE CON BASE\",\"quantity\":1,\"unitPrice\":22000.00,\"billingIndicator\":1}]}", null, "Mueblería Consumo", new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 13);
        }
    }
}
