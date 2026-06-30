using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZynstormECFPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingBusinessTypeSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessSimulationSample",
                columns: new[] { "BusinessSimulationSampleId", "BusinessTypeId", "DeletedTimeUtc", "Description", "EcfType", "GuidId", "IsDgiiApproved", "JsonData", "LastUpdateUtc", "Name", "RegisteredAt" },
                values: new object[,]
                {
                    { 34, 2, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0002-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 35, 2, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0002-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 36, 2, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0002-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 37, 2, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0002-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Farmacia - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 38, 2, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0002-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 39, 2, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0002-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 40, 2, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0002-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 41, 2, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0002-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Farmacia - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 42, 2, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0002-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Farmacia - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 43, 3, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0003-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 44, 3, null, "Muestra generada (plantilla validada) para Tipo 32.", "32", "00000000-0000-0000-0003-000000000032", false, "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 32", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 45, 3, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0003-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 46, 3, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0003-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 47, 3, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0003-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Repuesto - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 48, 3, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0003-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 49, 3, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0003-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 50, 3, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0003-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 51, 3, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0003-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Repuesto - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 52, 3, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0003-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Repuesto - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 53, 4, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0004-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 54, 4, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0004-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 55, 4, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0004-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 56, 4, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0004-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Taller de Mecánica - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 57, 4, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0004-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 58, 4, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0004-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 59, 4, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0004-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 60, 4, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0004-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Taller de Mecánica - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 61, 4, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0004-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Taller de Mecánica - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 62, 5, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0005-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 63, 5, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0005-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 64, 5, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0005-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 65, 5, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0005-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Surtidora - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 66, 5, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0005-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 67, 5, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0005-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 68, 5, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0005-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 69, 5, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0005-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Surtidora - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 70, 5, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0005-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Surtidora - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 71, 7, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0007-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 72, 7, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0007-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 73, 7, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0007-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 74, 7, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0007-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Tienda de pintura - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 75, 7, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0007-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 76, 7, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0007-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 77, 7, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0007-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 78, 7, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0007-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Tienda de pintura - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 79, 7, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0007-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Tienda de pintura - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 80, 8, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0008-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Boutique - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 81, 8, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0008-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Boutique - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 82, 8, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0008-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Boutique - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 83, 8, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0008-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Boutique - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 84, 8, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0008-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Boutique - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 85, 8, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0008-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Boutique - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 86, 8, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0008-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Boutique - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 87, 8, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0008-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Boutique - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 88, 8, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0008-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Boutique - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 89, 9, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0009-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 90, 9, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0009-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 91, 9, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0009-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 92, 9, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0009-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Colchoneria - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 93, 9, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0009-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 94, 9, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0009-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 95, 9, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0009-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 96, 9, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0009-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Colchoneria - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 97, 9, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0009-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Colchoneria - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 98, 10, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0010-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 99, 10, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0010-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 100, 10, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0010-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 101, 10, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0010-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Restaurante - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 102, 10, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0010-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 103, 10, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0010-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 104, 10, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0010-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 105, 10, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0010-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Restaurante - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 106, 10, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0010-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Restaurante - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 107, 11, null, "Muestra generada (plantilla validada) para Tipo 31.", "31", "00000000-0000-0000-0011-000000000031", false, "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 31", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 108, 11, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0011-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 109, 11, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0011-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 110, 11, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0011-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Cafeteria - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 111, 11, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0011-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 112, 11, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0011-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 113, 11, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0011-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 114, 11, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0011-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Cafeteria - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 115, 11, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0011-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Cafeteria - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 116, 12, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0012-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Tienda de electrodomésticos - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 117, 12, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0012-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Tienda de electrodomésticos - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 118, 12, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0012-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Tienda de electrodomésticos - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 119, 12, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0012-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Tienda de electrodomésticos - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 120, 12, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0012-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Tienda de electrodomésticos - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 121, 12, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0012-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Tienda de electrodomésticos - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 122, 12, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0012-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Tienda de electrodomésticos - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 123, 12, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0012-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Tienda de electrodomésticos - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 124, 13, null, "Muestra generada (plantilla validada) para Tipo 33.", "33", "00000000-0000-0000-0013-000000000033", false, "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}", null, "Mueblería - Tipo 33", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 125, 13, null, "Muestra generada (plantilla validada) para Tipo 34.", "34", "00000000-0000-0000-0013-000000000034", false, "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}", null, "Mueblería - Tipo 34", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 126, 13, null, "Muestra generada (plantilla validada) para Tipo 41.", "41", "00000000-0000-0000-0013-000000000041", false, "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}", null, "Mueblería - Tipo 41", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 127, 13, null, "Muestra generada (plantilla validada) para Tipo 43.", "43", "00000000-0000-0000-0013-000000000043", false, "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}", null, "Mueblería - Tipo 43", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 128, 13, null, "Muestra generada (plantilla validada) para Tipo 44.", "44", "00000000-0000-0000-0013-000000000044", false, "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}", null, "Mueblería - Tipo 44", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 129, 13, null, "Muestra generada (plantilla validada) para Tipo 45.", "45", "00000000-0000-0000-0013-000000000045", false, "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}", null, "Mueblería - Tipo 45", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 130, 13, null, "Muestra generada (plantilla validada) para Tipo 46.", "46", "00000000-0000-0000-0013-000000000046", false, "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}", null, "Mueblería - Tipo 46", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) },
                    { 131, 13, null, "Muestra generada (plantilla validada) para Tipo 47.", "47", "00000000-0000-0000-0013-000000000047", false, "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}", null, "Mueblería - Tipo 47", new DateTime(2026, 5, 6, 20, 0, 0, 0, DateTimeKind.Local) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "BusinessSimulationSample",
                keyColumn: "BusinessSimulationSampleId",
                keyValue: 131);
        }
    }
}
