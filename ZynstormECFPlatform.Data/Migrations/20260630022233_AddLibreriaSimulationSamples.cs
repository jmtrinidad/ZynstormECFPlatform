using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLibreriaSimulationSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "Description", "EcfType", "GuidId", "IsDgiiApproved", "JsonData", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 25, 6, null, "Ejemplo validado para Tipo 31.", "31", "98765432-1234-5678-90ab-cdef12345625", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"RESMA PAPEL BOND 8.5X11\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Librería - Factura de Crédito Fiscal", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 26, 6, null, "Ejemplo validado para Tipo 33.", "33", "98765432-1234-5678-90ab-cdef12345626", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE DEVOLUCION UTILES\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Librería - Nota de Crédito", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 27, 6, null, "Ejemplo validado para Tipo 34.", "34", "98765432-1234-5678-90ab-cdef12345627", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL PAPELERIA\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Librería - Nota de Débito", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 28, 6, null, "Ejemplo validado para Tipo 41.", "41", "98765432-1234-5678-90ab-cdef12345628", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"SERVICIO DE ENCUADERNACION\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Librería - Comprobante de Compras", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 29, 6, null, "Ejemplo validado para Tipo 43.", "43", "98765432-1234-5678-90ab-cdef12345629", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"REEMBOLSO CAJA CHICA PAPELERIA\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Librería - Gastos Menores", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 30, 6, null, "Ejemplo validado para Tipo 44.", "44", "98765432-1234-5678-90ab-cdef12345630", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LIBRO TEXTO EDUCATIVO\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Librería - Regímenes Especiales", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 31, 6, null, "Ejemplo validado para Tipo 45.", "45", "98765432-1234-5678-90ab-cdef12345631", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"KIT UTILES ESCOLARES\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Librería - Gubernamental", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 32, 6, null, "Ejemplo validado para Tipo 46.", "46", "98765432-1234-5678-90ab-cdef12345632", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"LIBROS EDUCATIVOS EXPORTACION\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Librería - Exportación", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 33, 6, null, "Ejemplo validado para Tipo 47.", "47", "98765432-1234-5678-90ab-cdef12345633", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"REGALIAS EDITORIALES EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Librería - Pagos Exterior", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 33);
        }
    }
}
