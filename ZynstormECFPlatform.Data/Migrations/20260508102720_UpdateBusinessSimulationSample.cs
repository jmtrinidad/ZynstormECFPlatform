using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBusinessSimulationSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BusinessSimulationSample",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "BusinessSimulationSample",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                columns: new[] { "Description", "JsonData", "Name" },
                values: new object[] { "Ejemplo validado para Tipo 31.", "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", "Factura de Crédito Fiscal" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de medicamentos.", "Farmacia Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Servicios de mantenimiento.", "Taller Mecánico Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de productos de consumo.", "Surtidora Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 6,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de útiles escolares.", "Librería Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 7,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de pinturas.", "Tienda Pintura Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 8,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de ropa y calzado.", "Boutique Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 9,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de artículos de descanso.", "Colchoneria Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 10,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Servicios de comida.", "Restaurante Consumo" });

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 11,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Venta de café y postres.", "Cafeteria Consumo" });

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "Description", "EcfType", "GuidId", "JsonData", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 12, 1, null, "Ejemplo validado para Tipo 32 con monto >= 250k.", "32", "98765432-1234-5678-90ab-cdef12345612", "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4}]}", null, "Factura de Consumo (Gran Monto)", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 13, 1, null, "Ejemplo validado para Tipo 33.", "33", "98765432-1234-5678-90ab-cdef12345613", "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Nota de Crédito", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 14, 1, null, "Ejemplo validado para Tipo 34.", "34", "98765432-1234-5678-90ab-cdef12345614", "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Nota de Débito", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 15, 1, null, "Ejemplo validado para Tipo 41.", "41", "98765432-1234-5678-90ab-cdef12345615", "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Comprobante de Compras", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 16, 1, null, "Ejemplo validado para Tipo 43.", "43", "98765432-1234-5678-90ab-cdef12345616", "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Gastos Menores", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 17, 1, null, "Ejemplo validado para Tipo 44.", "44", "98765432-1234-5678-90ab-cdef12345617", "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Regímenes Especiales", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 18, 1, null, "Ejemplo validado para Tipo 45.", "45", "98765432-1234-5678-90ab-cdef12345618", "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Gubernamental", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 19, 1, null, "Ejemplo validado para Tipo 46.", "46", "98765432-1234-5678-90ab-cdef12345619", "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Exportación", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 20, 1, null, "Ejemplo validado para Tipo 47.", "47", "98765432-1234-5678-90ab-cdef12345620", "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Pagos Exterior", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 20);

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BusinessSimulationSample");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "BusinessSimulationSample");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 1,
                column: "JsonData",
                value: "{\"IssuerRnc\":\"133009889\",\"IssuerName\":\"Transporte NJ, SRL\",\"IssuerAddress\":\"Ensanche Gregorio Luperon, Santiago\",\"CustomerRnc\":\"102620717\",\"CustomerName\":\"Morteros de Europa\",\"Items\":[{\"Name\":\"Servicio de Transporte de Carga\",\"Quantity\":1,\"UnitPrice\":6000.00,\"TaxPercentage\":0,\"BillingIndicator\":4}]}");

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "EcfType", "GuidId", "JsonData", "LastUpdateUtc", "RegisteredAt" },
                values: new object[] { 3, 3, null, "32", "98765432-1234-5678-90ab-cdef12345603", "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Repuestos El Motor\",\"IssuerAddress\":\"Calle Duarte 45\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Filtro de Aceite\",\"Quantity\":1,\"UnitPrice\":650.00,\"TaxPercentage\":18},{\"Name\":\"Aceite Sintético 5W30\",\"Quantity\":4,\"UnitPrice\":850.00,\"TaxPercentage\":18}]}", null, new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
