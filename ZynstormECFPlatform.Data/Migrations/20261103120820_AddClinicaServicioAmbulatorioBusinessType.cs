using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicaServicioAmbulatorioBusinessType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessType",
                columns: new[] { "BusinessTypeId", "DeletedTimeUtc", "Description", "GuidId", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[] { 14, null, "Consultas, procedimientos y servicios de salud ambulatorios (mayormente exentos de ITBIS).", "0c1d2e3f-4a5b-6c7d-8e9f-0a1b2c3d4e5f", null, "Clínica/Servicio Ambulatorio", new DateTime(2026, 11, 2, 20, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "Description", "EcfType", "GuidId", "IsDgiiApproved", "JsonData", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 132, 14, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0014-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CONSULTA MEDICA GENERAL\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 133, 14, null, "Muestra generada (plantilla validada) para Tipo 32.", "32", "00000000-0000-0000-0014-000000000032", false, "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROCEDIMIENTO AMBULATORIO\",\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 32", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 134, 14, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0014-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE SERVICIOS MEDICOS\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 135, 14, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0014-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL LABORATORIO\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 136, 14, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0014-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIO DE LIMPIEZA CLINICA\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 137, 14, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0014-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"REEMBOLSO CAJA CHICA INSUMOS MEDICOS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 138, 14, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0014-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CONSULTA ESPECIALIZADA\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 139, 14, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0014-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIOS DE EMERGENCIA AMBULATORIA\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 140, 14, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0014-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"SERVICIOS MEDICOS A PACIENTES EXTRANJEROS\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Clínica/Servicio Ambulatorio - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 141, 14, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0014-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"HONORARIOS MEDICOS EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Clínica/Servicio Ambulatorio - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 14);
        }
    }
}
