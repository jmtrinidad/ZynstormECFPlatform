using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClinicaSimulationServiceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 132,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CONSULTA MEDICA GENERAL\",\"itemType\":2,\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4},{\"name\":\"LABORATORIO CLINICO HEMOGRAMA COMPLETO\",\"quantity\":1,\"unitPrice\":650.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"ELECTROCARDIOGRAMA 12 DERIVACIONES\",\"quantity\":1,\"unitPrice\":900.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"CURACION Y SUTURA MENOR\",\"quantity\":1,\"unitPrice\":1200.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 133,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROCEDIMIENTO AMBULATORIO\",\"itemType\":2,\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4},{\"name\":\"RADIOGRAFIA DE TORAX\",\"quantity\":1,\"unitPrice\":1800.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"TERAPIA FISICA SESION\",\"quantity\":3,\"unitPrice\":1000.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 134,
                column: "JsonData",
                value: "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE SERVICIOS MEDICOS\",\"itemType\":2,\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 135,
                column: "JsonData",
                value: "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL LABORATORIO\",\"itemType\":2,\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 136,
                column: "JsonData",
                value: "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIO DE LIMPIEZA CLINICA\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 137,
                column: "JsonData",
                value: "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"REEMBOLSO CAJA CHICA INSUMOS MEDICOS\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 138,
                column: "JsonData",
                value: "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CONSULTA ESPECIALIZADA\",\"itemType\":2,\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4},{\"name\":\"SONOGRAFIA ABDOMINAL\",\"quantity\":1,\"unitPrice\":2500.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"NEBULIZACION\",\"quantity\":2,\"unitPrice\":350.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 139,
                column: "JsonData",
                value: "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIOS DE EMERGENCIA AMBULATORIA\",\"itemType\":2,\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4},{\"name\":\"VACUNACION Y ADMINISTRACION DE INYECTABLES\",\"quantity\":2,\"unitPrice\":400.00,\"billingIndicator\":4,\"itemType\":2},{\"name\":\"ELECTROCARDIOGRAMA 12 DERIVACIONES\",\"quantity\":1,\"unitPrice\":900.00,\"billingIndicator\":4,\"itemType\":2}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 140,
                column: "JsonData",
                value: "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"SERVICIOS MEDICOS A PACIENTES EXTRANJEROS\",\"itemType\":2,\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 141,
                column: "JsonData",
                value: "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"HONORARIOS MEDICOS EXTERIOR\",\"itemType\":2,\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 132,
                column: "JsonData",
                value: "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CONSULTA MEDICA GENERAL\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 133,
                column: "JsonData",
                value: "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROCEDIMIENTO AMBULATORIO\",\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 134,
                column: "JsonData",
                value: "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE SERVICIOS MEDICOS\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 135,
                column: "JsonData",
                value: "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL LABORATORIO\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 136,
                column: "JsonData",
                value: "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIO DE LIMPIEZA CLINICA\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 137,
                column: "JsonData",
                value: "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"REEMBOLSO CAJA CHICA INSUMOS MEDICOS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 138,
                column: "JsonData",
                value: "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CONSULTA ESPECIALIZADA\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 139,
                column: "JsonData",
                value: "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIOS DE EMERGENCIA AMBULATORIA\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 140,
                column: "JsonData",
                value: "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"SERVICIOS MEDICOS A PACIENTES EXTRANJEROS\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}");

            migrationBuilder.UpdateData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 141,
                column: "JsonData",
                value: "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"HONORARIOS MEDICOS EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}");
        }
    }
}
