using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRentaVehiculosBusinessType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessType",
                columns: new[] { "BusinessTypeId", "DeletedTimeUtc", "Description", "GuidId", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[] { 15, null, "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%).", "1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a", null, "Renta de Vehículos", new DateTime(2026, 11, 12, 20, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "Description", "EcfType", "GuidId", "IsDgiiApproved", "JsonData", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 142, 15, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0015-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER KIA PICANTO 2025 (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":1},{\"name\":\"ALQUILER NISSAN SENTRA SV (DIA)\",\"quantity\":1,\"unitPrice\":2100.00,\"billingIndicator\":1,\"itemType\":2},{\"name\":\"ALQUILER SCOOTER CFMOTO 150CC (DIA)\",\"quantity\":1,\"unitPrice\":900.00,\"billingIndicator\":1,\"itemType\":2},{\"name\":\"ENTREGA Y RECOGIDA A DOMICILIO\",\"quantity\":1,\"unitPrice\":500.00,\"billingIndicator\":1,\"itemType\":2}]}", null, "Renta de Vehículos - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 143, 15, null, "Muestra generada (plantilla validada) para Tipo 32.", "32", "00000000-0000-0000-0015-000000000032", false, "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER BUGGY CF MOTO UFORCE 600 (DIA)\",\"itemType\":2,\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":1},{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00,\"billingIndicator\":1,\"itemType\":2},{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00,\"billingIndicator\":1,\"itemType\":2}]}", null, "Renta de Vehículos - Tipo 32", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 144, 15, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0015-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE POR DIAS DE ALQUILER\",\"itemType\":2,\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":1}]}", null, "Renta de Vehículos - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 145, 15, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0015-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE\",\"itemType\":2,\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":1}]}", null, "Renta de Vehículos - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 146, 15, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0015-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"MANTENIMIENTO Y LAVADO DE FLOTA\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1}]}", null, "Renta de Vehículos - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 147, 15, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0015-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"REEMBOLSO CAJA CHICA COMBUSTIBLE\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Renta de Vehículos - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 148, 15, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0015-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER SCOOTER TAURO DIO 110 (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4},{\"name\":\"ALQUILER SYM FNX 200CC (DIA)\",\"quantity\":1,\"unitPrice\":900.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"ENTREGA Y RECOGIDA A DOMICILIO\",\"quantity\":1,\"unitPrice\":500.00,\"billingIndicator\":4,\"itemType\":2}]}", null, "Renta de Vehículos - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 149, 15, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0015-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER SCOOTER SYM FNX 200CC (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":1},{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00,\"billingIndicator\":1,\"itemType\":2},{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00,\"billingIndicator\":1,\"itemType\":2}]}", null, "Renta de Vehículos - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 150, 15, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0015-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"ALQUILER DE VEHICULO A TURISTA EXTRANJERO\",\"itemType\":2,\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Renta de Vehículos - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 151, 15, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0015-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"ALQUILER DE VEHICULO EN EL EXTERIOR\",\"itemType\":2,\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Renta de Vehículos - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 15);
        }
    }
}
