using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxPercentageToGravadoSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 21,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"NEVERA SAMSUNG BESPOKE 23 P3\",\"quantity\":1,\"unitPrice\":85000.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"TELEVISOR LG OLED 55\\\"\",\"quantity\":1,\"unitPrice\":65000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 22,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LICUADORA NINJA PROFESSIONAL\",\"quantity\":1,\"unitPrice\":8500.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"FREIDORA DE AIRE DIGITAL 5.5L\",\"quantity\":1,\"unitPrice\":7200.00,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 23,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"MUEBLES Y DECORACIONES S.A.\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"JUEGO DE COMEDOR MODERNO 6 SILLAS\",\"quantity\":1,\"unitPrice\":45000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 24,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SOFA SECCIONAL EN TELA GRIS\",\"quantity\":1,\"unitPrice\":38000.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"CAMA QUEEN SIZE CON BASE\",\"quantity\":1,\"unitPrice\":22000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 142,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER KIA PICANTO 2025 (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"ALQUILER NISSAN SENTRA SV (DIA)\",\"quantity\":1,\"unitPrice\":2100.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2},{\"name\":\"ALQUILER SCOOTER CFMOTO 150CC (DIA)\",\"quantity\":1,\"unitPrice\":900.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2},{\"name\":\"ENTREGA Y RECOGIDA A DOMICILIO\",\"quantity\":1,\"unitPrice\":500.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 143,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER BUGGY CF MOTO UFORCE 600 (DIA)\",\"itemType\":2,\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2},{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 144,
                column: "JsonData",
                value: "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE POR DIAS DE ALQUILER\",\"itemType\":2,\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 145,
                column: "JsonData",
                value: "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE\",\"itemType\":2,\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 146,
                column: "JsonData",
                value: "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"MANTENIMIENTO Y LAVADO DE FLOTA\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 149,
                column: "JsonData",
                value: "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER SCOOTER SYM FNX 200CC (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2},{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00,\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 15,
                column: "Description",
                value: "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 21,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"NEVERA SAMSUNG BESPOKE 23 P3\",\"quantity\":1,\"unitPrice\":85000.00,\"billingIndicator\":1},{\"name\":\"TELEVISOR LG OLED 55\\\"\",\"quantity\":1,\"unitPrice\":65000.00,\"billingIndicator\":1}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 22,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LICUADORA NINJA PROFESSIONAL\",\"quantity\":1,\"unitPrice\":8500.00,\"billingIndicator\":1},{\"name\":\"FREIDORA DE AIRE DIGITAL 5.5L\",\"quantity\":1,\"unitPrice\":7200.00,\"billingIndicator\":1}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 23,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"MUEBLES Y DECORACIONES S.A.\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"JUEGO DE COMEDOR MODERNO 6 SILLAS\",\"quantity\":1,\"unitPrice\":45000.00,\"billingIndicator\":1}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 24,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SOFA SECCIONAL EN TELA GRIS\",\"quantity\":1,\"unitPrice\":38000.00,\"billingIndicator\":1},{\"name\":\"CAMA QUEEN SIZE CON BASE\",\"quantity\":1,\"unitPrice\":22000.00,\"billingIndicator\":1}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 142,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER KIA PICANTO 2025 (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4},{\"name\":\"ALQUILER NISSAN SENTRA SV (DIA)\",\"quantity\":1,\"unitPrice\":2100.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"ALQUILER SCOOTER CFMOTO 150CC (DIA)\",\"quantity\":1,\"unitPrice\":900.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"ENTREGA Y RECOGIDA A DOMICILIO\",\"quantity\":1,\"unitPrice\":500.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 143,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER BUGGY CF MOTO UFORCE 600 (DIA)\",\"itemType\":2,\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4},{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 144,
                column: "JsonData",
                value: "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE POR DIAS DE ALQUILER\",\"itemType\":2,\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 145,
                column: "JsonData",
                value: "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE\",\"itemType\":2,\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 146,
                column: "JsonData",
                value: "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"MANTENIMIENTO Y LAVADO DE FLOTA\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 149,
                column: "JsonData",
                value: "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"ALQUILER SCOOTER SYM FNX 200CC (DIA)\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4},{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessType",
                keyColumn: "BusinessTypeId",
                keyValue: 15,
                column: "Description",
                value: "Alquiler de vehículos, motos y buggies por día.");
        }
    }
}
