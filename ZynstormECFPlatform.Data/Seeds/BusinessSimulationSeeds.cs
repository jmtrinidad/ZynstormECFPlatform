using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Seeds;

public static class BusinessSimulationSeeds
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var businessTypes = new List<BusinessType>
        {
            new BusinessType { BusinessTypeId = 1, Name = "Transporte", Description = "Servicios de transporte de carga y logística.", GuidId = "7a8b9c0d-1e2f-3g4h-5i6j-7k8l9m0n1o2p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") },
            new BusinessType { BusinessTypeId = 2, Name = "Farmacia", Description = "Venta de medicamentos y productos de salud.", GuidId = "8a9b0c1d-2e3f-4g5h-6i7j-8k9l0m1n2o3p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") },
            new BusinessType { BusinessTypeId = 3, Name = "Repuesto", Description = "Venta de piezas y accesorios para vehículos.", GuidId = "9a0b1c2d-3e4f-5g6h-7i8j-9k0l1m2n3o4p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") },
            new BusinessType { BusinessTypeId = 4, Name = "Taller de Mecánica", Description = "Servicios de mantenimiento y reparación de vehículos.", GuidId = "0a1b2c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") },
            new BusinessType { BusinessTypeId = 5, Name = "Surtidora", Description = "Venta al por mayor y detalle de productos de consumo.", GuidId = "1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") },
            new BusinessType { BusinessTypeId = 6, Name = "Librerías", Description = "Venta de libros, útiles escolares y papelería.", GuidId = "2a3b4c5d-6e7f-8g9h-0i1j-2k3l4m5n6o7p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") },
            new BusinessType { BusinessTypeId = 7, Name = "Tienda de pintura", Description = "Venta de pinturas, barnices y accesorios.", GuidId = "3a4b5c6d-7e8f-9g0h-1i2j-3k4l5m6n7o8p", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z") },
            new BusinessType { BusinessTypeId = 8, Name = "Boutique", Description = "Venta de ropa, calzado y accesorios de moda.", GuidId = "4a5b6c7d-8e9f-0g1h-2i3j-4k5l6m7n8o9p", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z") },
            new BusinessType { BusinessTypeId = 9, Name = "Colchoneria", Description = "Venta de colchones, almohadas y artículos de descanso.", GuidId = "5a6b7c8d-9e0f-1g2h-3i4j-5k6l7m8n9o0p", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z") },
            new BusinessType { BusinessTypeId = 10, Name = "Restaurante", Description = "Servicios de comida y bebidas preparadas.", GuidId = "6a7b8c9d-0e1f-2g3h-4i5j-6k7l8m9n0o1p", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z") },
            new BusinessType { BusinessTypeId = 11, Name = "Cafeteria", Description = "Venta de café, postres y comidas ligeras.", GuidId = "7a8b9c0d-1e2f-3g4h-5i6j-7k8l9m0n1o2p", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z") },
            new BusinessType { BusinessTypeId = 12, Name = "Tienda de electrodomésticos", Description = "Venta de equipos para el hogar y dispositivos electrónicos.", GuidId = "8a9b0c1d-1e2f-3g4h-5i6j-8k9l0m1n2o3q", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z") },
            new BusinessType { BusinessTypeId = 13, Name = "Mueblería", Description = "Venta de muebles, decoración y artículos para el hogar.", GuidId = "9b0c1d2e-3f4g-5h6i-7j8k-9l0m1n2o3p4r", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z") },
            new BusinessType { BusinessTypeId = 14, Name = "Clínica/Servicio Ambulatorio", Description = "Consultas, procedimientos y servicios de salud ambulatorios (mayormente exentos de ITBIS).", GuidId = "0c1d2e3f-4a5b-6c7d-8e9f-0a1b2c3d4e5f", RegisteredAt = DateTime.Parse("2026-11-03T00:00:00Z") },
            new BusinessType { BusinessTypeId = 15, Name = "Renta de Vehículos", Description = "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%).", GuidId = "1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a", RegisteredAt = DateTime.Parse("2026-11-13T00:00:00Z") }
        };

        modelBuilder.Entity<BusinessType>().HasData(businessTypes);

        var samples = new List<BusinessSimulationSample>
        {
            // --- TRANSPORTE (Full Certification Samples) ---
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 1, BusinessTypeId = 1, EcfType = "31", 
                Name = "Factura de Crédito Fiscal", Description = "Ejemplo validado para Tipo 31.",
                GuidId = "98765432-1234-5678-90ab-cdef12345601", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"CEDEBRAL 5000 JARABE\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 12, BusinessTypeId = 1, EcfType = "32", 
                Name = "Factura de Consumo (Gran Monto)", Description = "Ejemplo validado para Tipo 32 con monto >= 250k.",
                GuidId = "98765432-1234-5678-90ab-cdef12345612", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E320000000001\",\"customerRnc\":\"40208719662\",\"customerName\":\"BRYAN TORRES\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":2,\"unitPrice\":300000.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 13, BusinessTypeId = 1, EcfType = "33", 
                Name = "Nota de Crédito", Description = "Ejemplo validado para Tipo 33.",
                GuidId = "98765432-1234-5678-90ab-cdef12345613", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"GENERAL\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 14, BusinessTypeId = 1, EcfType = "34", 
                Name = "Nota de Débito", Description = "Ejemplo validado para Tipo 34.",
                GuidId = "98765432-1234-5678-90ab-cdef12345614", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CORACOR A C/30 TABS.\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 15, BusinessTypeId = 1, EcfType = "41",
                Name = "Comprobante de Compras", Description = "Ejemplo validado para Tipo 41.",
                GuidId = "98765432-1234-5678-90ab-cdef12345615", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 16, BusinessTypeId = 1, EcfType = "43", 
                Name = "Gastos Menores", Description = "Ejemplo validado para Tipo 43.",
                GuidId = "98765432-1234-5678-90ab-cdef12345616", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"PROPIETARIO COMPANIA DE TRANSPORTE DIVER\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 17, BusinessTypeId = 1, EcfType = "44", 
                Name = "Regímenes Especiales", Description = "Ejemplo validado para Tipo 44.",
                GuidId = "98765432-1234-5678-90ab-cdef12345617", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"GREEN PIGEON PEAS CARIDOM 24/15 OZ.\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 18, BusinessTypeId = 1, EcfType = "45", 
                Name = "Gubernamental", Description = "Ejemplo validado para Tipo 45.",
                GuidId = "98765432-1234-5678-90ab-cdef12345618", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"OXIGEN 200 C/30 TABS.\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 19, BusinessTypeId = 1, EcfType = "46", 
                Name = "Exportación", Description = "Ejemplo validado para Tipo 46.",
                GuidId = "98765432-1234-5678-90ab-cdef12345619", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"AGUACATE CRIOLLO\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 20, BusinessTypeId = 1, EcfType = "47", 
                Name = "Pagos Exterior", Description = "Ejemplo validado para Tipo 47.",
                GuidId = "98765432-1234-5678-90ab-cdef12345620", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"SERVICIO PROFESIONAL EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}"
            },

            // --- LIBRERÍAS (Full Certification Samples) ---
            // Clonadas del set validado de Transporte (Tipo 1): se conservan RNC de
            // comprador, montos, referencias y campos de exportación/moneda tal cual,
            // solo se temátican los nombres de los ítems. El tipo 32 ya existe (Id 6).
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 25, BusinessTypeId = 6, EcfType = "31",
                Name = "Librería - Factura de Crédito Fiscal", Description = "Ejemplo validado para Tipo 31.",
                GuidId = "98765432-1234-5678-90ab-cdef12345625", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"RESMA PAPEL BOND 8.5X11\",\"quantity\":1,\"unitPrice\":244.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 26, BusinessTypeId = 6, EcfType = "33",
                Name = "Librería - Nota de Crédito", Description = "Ejemplo validado para Tipo 33.",
                GuidId = "98765432-1234-5678-90ab-cdef12345626", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E330000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":1,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"AJUSTE DEVOLUCION UTILES\",\"quantity\":1,\"unitPrice\":203898.31,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 27, BusinessTypeId = 6, EcfType = "34",
                Name = "Librería - Nota de Débito", Description = "Ejemplo validado para Tipo 34.",
                GuidId = "98765432-1234-5678-90ab-cdef12345627", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E340000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"CLIENTES DE LA ADMINISTRACION\",\"incomeType\":\"01\",\"paymentType\":2,\"referenceNcf\":\"E310000000002\",\"referenceReasonCode\":3,\"items\":[{\"name\":\"CARGO ADICIONAL PAPELERIA\",\"quantity\":5,\"unitPrice\":601.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 28, BusinessTypeId = 6, EcfType = "41",
                Name = "Librería - Comprobante de Compras", Description = "Ejemplo validado para Tipo 41.",
                GuidId = "98765432-1234-5678-90ab-cdef12345628", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SERVICIO DE ENCUADERNACION\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 29, BusinessTypeId = 6, EcfType = "43",
                Name = "Librería - Gastos Menores", Description = "Ejemplo validado para Tipo 43.",
                GuidId = "98765432-1234-5678-90ab-cdef12345629", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E430000000001\",\"customerRnc\":\"\",\"customerName\":\"\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"REEMBOLSO CAJA CHICA PAPELERIA\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 30, BusinessTypeId = 6, EcfType = "44",
                Name = "Librería - Regímenes Especiales", Description = "Ejemplo validado para Tipo 44.",
                GuidId = "98765432-1234-5678-90ab-cdef12345630", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E440000000001\",\"customerRnc\":\"131098843\",\"customerName\":\"ZONA FRANCA 6 DE NOVIEMBRE SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LIBRO TEXTO EDUCATIVO\",\"quantity\":1,\"unitPrice\":29.50,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 31, BusinessTypeId = 6, EcfType = "45",
                Name = "Librería - Gubernamental", Description = "Ejemplo validado para Tipo 45.",
                GuidId = "98765432-1234-5678-90ab-cdef12345631", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E450000000001\",\"customerRnc\":\"401506459\",\"customerName\":\"PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"KIT UTILES ESCOLARES\",\"quantity\":1,\"unitPrice\":1197.00,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 32, BusinessTypeId = 6, EcfType = "46",
                Name = "Librería - Exportación", Description = "Ejemplo validado para Tipo 46.",
                GuidId = "98765432-1234-5678-90ab-cdef12345632", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E460000000001\",\"customerRnc\":\"131880681\",\"customerName\":\"ZONA FRANCA LOI\",\"customerForeignId\":\"533445888\",\"customerCountry\":\"PUERTO RICO\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"LIBROS EDUCATIVOS EXPORTACION\",\"quantity\":100,\"unitPrice\":18000.00,\"billingIndicator\":3}],\"exportRegimenAduanero\":\"EXPORTACION NACIONAL\",\"transpViaTransporte\":\"02\",\"transpPaisDestino\":\"PUERTO RICO\"}"
            },
            new BusinessSimulationSample
            {
                BusinessSimulationSampleId = 33, BusinessTypeId = 6, EcfType = "47",
                Name = "Librería - Pagos Exterior", Description = "Ejemplo validado para Tipo 47.",
                GuidId = "98765432-1234-5678-90ab-cdef12345633", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ncf\":\"E470000000001\",\"customerForeignId\":\"533445888\",\"customerName\":\"ALEJA FERMIN SANTOS\",\"currencyTipoMoneda\":\"USD\",\"currencyTipoCambio\":60.0,\"items\":[{\"name\":\"REGALIAS EDITORIALES EXTERIOR\",\"quantity\":1,\"unitPrice\":3000.0,\"billingIndicator\":4}]}"
            },

            // --- Other business types fallback samples ---
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 2, BusinessTypeId = 2, EcfType = "32", 
                Name = "Farmacia Consumo", Description = "Venta de medicamentos.",
                GuidId = "98765432-1234-5678-90ab-cdef12345602", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Farmacia Salud\",\"IssuerAddress\":\"Av. 27 de Febrero 123\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Amoxicilina 500mg\",\"Quantity\":1,\"UnitPrice\":450.00,\"TaxPercentage\":0},{\"Name\":\"Vitamina C 1000mg\",\"Quantity\":2,\"UnitPrice\":300.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 4, BusinessTypeId = 4, EcfType = "32", 
                Name = "Taller Mecánico Consumo", Description = "Servicios de mantenimiento.",
                GuidId = "98765432-1234-5678-90ab-cdef12345604", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Taller Los Amigos\",\"IssuerAddress\":\"Av. Imbert 78\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cambio de Aceite (Labor)\",\"Quantity\":1,\"UnitPrice\":1500.00,\"TaxPercentage\":18},{\"Name\":\"Revisión de Frenos\",\"Quantity\":1,\"UnitPrice\":800.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 5, BusinessTypeId = 5, EcfType = "32", 
                Name = "Surtidora Consumo", Description = "Venta de productos de consumo.",
                GuidId = "98765432-1234-5678-90ab-cdef12345605", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Surtidora El Pueblo\",\"IssuerAddress\":\"Calle Central 10\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Refresco 2L\",\"Quantity\":12,\"UnitPrice\":75.00,\"TaxPercentage\":18},{\"Name\":\"Arroz 10lb\",\"Quantity\":5,\"UnitPrice\":350.00,\"TaxPercentage\":0}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 6, BusinessTypeId = 6, EcfType = "32", 
                Name = "Librería Consumo", Description = "Venta de útiles escolares.",
                GuidId = "98765432-1234-5678-90ab-cdef12345606", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Librería Minerva\",\"IssuerAddress\":\"Calle Independencia 55\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cuaderno A4\",\"Quantity\":5,\"UnitPrice\":120.00,\"TaxPercentage\":18},{\"Name\":\"Lápiz de Grafito\",\"Quantity\":20,\"UnitPrice\":15.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 7, BusinessTypeId = 7, EcfType = "32", 
                Name = "Tienda Pintura Consumo", Description = "Venta de pinturas.",
                GuidId = "98765432-1234-5678-90ab-cdef12345671", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Pinturas del Este\",\"IssuerAddress\":\"Av. Independencia 456\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cubeta Pintura Blanca Satinada\",\"Quantity\":1,\"UnitPrice\":2000.00,\"TaxPercentage\":18},{\"Name\":\"Brocha 4 Pulgadas Profesional\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 8, BusinessTypeId = 8, EcfType = "32", 
                Name = "Boutique Consumo", Description = "Venta de ropa y calzado.",
                GuidId = "98765432-1234-5678-90ab-cdef12345672", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Boutique Elegance\",\"IssuerAddress\":\"Calle del Sol 789\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Vestido de Gala Azul\",\"Quantity\":1,\"UnitPrice\":3500.00,\"TaxPercentage\":18},{\"Name\":\"Cinturón Cuero Genuino\",\"Quantity\":1,\"UnitPrice\":1000.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 9, BusinessTypeId = 9, EcfType = "32", 
                Name = "Colchoneria Consumo", Description = "Venta de artículos de descanso.",
                GuidId = "98765432-1234-5678-90ab-cdef12345673", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Colchones Confort\",\"IssuerAddress\":\"Av. Winston Churchill 101\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Colchón King Size Ortopédico\",\"Quantity\":1,\"UnitPrice\":12000.00,\"TaxPercentage\":18},{\"Name\":\"Almohada Memory Foam\",\"Quantity\":2,\"UnitPrice\":1500.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 10, BusinessTypeId = 10, EcfType = "32", 
                Name = "Restaurante Consumo", Description = "Servicios de comida.",
                GuidId = "98765432-1234-5678-90ab-cdef12345674", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Restaurante Sabores\",\"IssuerAddress\":\"Calle Gourmet 202\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cena Especial del Chef (Dúo)\",\"Quantity\":1,\"UnitPrice\":2500.00,\"TaxPercentage\":18},{\"Name\":\"Botella de Vino Tinto Reserva\",\"Quantity\":1,\"UnitPrice\":700.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 11, BusinessTypeId = 11, EcfType = "32", 
                Name = "Cafeteria Consumo", Description = "Venta de café y postres.",
                GuidId = "98765432-1234-5678-90ab-cdef12345675", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Café Aroma\",\"IssuerAddress\":\"Plaza Central Local 5\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Café Latte Grande\",\"Quantity\":2,\"UnitPrice\":175.00,\"TaxPercentage\":18},{\"Name\":\"Croissant de Almendras\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}"
            },
            // --- TIENDA DE ELECTRODOMÉSTICOS ---
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 21, BusinessTypeId = 12, EcfType = "31", 
                Name = "Tienda Electrodomésticos Crédito Fiscal", Description = "Ejemplo validado para Tipo 31.",
                GuidId = "98765432-1234-5678-90ab-cdef12345621", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"NEVERA SAMSUNG BESPOKE 23 P3\",\"quantity\":1,\"unitPrice\":85000.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"TELEVISOR LG OLED 55\\\"\",\"quantity\":1,\"unitPrice\":65000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 22, BusinessTypeId = 12, EcfType = "32", 
                Name = "Tienda Electrodomésticos Consumo", Description = "Ejemplo validado para Tipo 32.",
                GuidId = "98765432-1234-5678-90ab-cdef12345622", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LICUADORA NINJA PROFESSIONAL\",\"quantity\":1,\"unitPrice\":8500.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"FREIDORA DE AIRE DIGITAL 5.5L\",\"quantity\":1,\"unitPrice\":7200.00,\"billingIndicator\":1,\"taxPercentage\":18}]}"
            },
            // --- MUEBLERÍA ---
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 23, BusinessTypeId = 13, EcfType = "31", 
                Name = "Mueblería Crédito Fiscal", Description = "Ejemplo validado para Tipo 31.",
                GuidId = "98765432-1234-5678-90ab-cdef12345623", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E310000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"MUEBLES Y DECORACIONES S.A.\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"JUEGO DE COMEDOR MODERNO 6 SILLAS\",\"quantity\":1,\"unitPrice\":45000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 24, BusinessTypeId = 13, EcfType = "32", 
                Name = "Mueblería Consumo", Description = "Ejemplo validado para Tipo 32.",
                GuidId = "98765432-1234-5678-90ab-cdef12345624", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SOFA SECCIONAL EN TELA GRIS\",\"quantity\":1,\"unitPrice\":38000.00,\"billingIndicator\":1,\"taxPercentage\":18},{\"name\":\"CAMA QUEEN SIZE CON BASE\",\"quantity\":1,\"unitPrice\":22000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}"
            }
        };

        // Completar el set para TODOS los tipos de negocio reusando las plantillas
        // validadas de Transporte (Tipo 1). Solo se agregan los tipos e-CF que falten
        // para cada negocio (evita duplicar la clave EcfType, que rompería el
        // ToDictionary del servicio de simulación). Datos deterministas para que
        // no se generen migraciones espurias.
        var requiredTypes = new[] { "31", "32", "33", "34", "41", "43", "44", "45", "46", "47" };
        var templatesByType = samples
            .Where(s => s.BusinessTypeId == 1)
            .ToDictionary(s => s.EcfType, s => s.JsonData);
        var existingPairs = samples
            .Select(s => (s.BusinessTypeId, s.EcfType))
            .ToHashSet();

        // Clínica/Servicio Ambulatorio (Id 14): mismas plantillas validadas de Transporte,
        // solo se temátican los nombres de los ítems (los montos, RNC e indicadores de
        // facturación se conservan; 4 = Exento, como en los servicios de salud).
        var clinicItemNames = new Dictionary<string, (string From, string To)>
        {
            ["31"] = ("CEDEBRAL 5000 JARABE", "CONSULTA MEDICA GENERAL"),
            ["32"] = ("GREEN PIGEON PEAS CARIDOM 24/15 OZ.", "PROCEDIMIENTO AMBULATORIO"),
            ["33"] = ("GENERAL", "AJUSTE SERVICIOS MEDICOS"),
            ["34"] = ("CORACOR A C/30 TABS.", "CARGO ADICIONAL LABORATORIO"),
            ["41"] = ("COMISION VERIFON TARJETAS", "SERVICIO DE LIMPIEZA CLINICA"),
            ["43"] = ("PROPIETARIO COMPANIA DE TRANSPORTE DIVER", "REEMBOLSO CAJA CHICA INSUMOS MEDICOS"),
            ["44"] = ("GREEN PIGEON PEAS CARIDOM 24/15 OZ.", "CONSULTA ESPECIALIZADA"),
            ["45"] = ("OXIGEN 200 C/30 TABS.", "SERVICIOS DE EMERGENCIA AMBULATORIA"),
            ["46"] = ("AGUACATE CRIOLLO", "SERVICIOS MEDICOS A PACIENTES EXTRANJEROS"),
            ["47"] = ("SERVICIO PROFESIONAL EXTERIOR", "HONORARIOS MEDICOS EXTERIOR"),
        };

        // Servicios ambulatorios adicionales (itemType 2 = Servicio, exentos de ITBIS) que se
        // agregan después del ítem principal en los tipos con detalle libre. Los tipos 33/34
        // (notas) y 41/43/46/47 conservan un solo ítem para no alterar sus reglas de referencia.
        const string clinicExempt = "\"billingIndicator\":4,\"itemType\":2}";
        var clinicExtraItems = new Dictionary<string, string>
        {
            ["31"] = "{\"name\":\"LABORATORIO CLINICO HEMOGRAMA COMPLETO\",\"quantity\":1,\"unitPrice\":650.00," + clinicExempt
                   + ",{\"name\":\"ELECTROCARDIOGRAMA 12 DERIVACIONES\",\"quantity\":1,\"unitPrice\":900.00," + clinicExempt
                   + ",{\"name\":\"CURACION Y SUTURA MENOR\",\"quantity\":1,\"unitPrice\":1200.00," + clinicExempt,
            ["32"] = "{\"name\":\"RADIOGRAFIA DE TORAX\",\"quantity\":1,\"unitPrice\":1800.00," + clinicExempt
                   + ",{\"name\":\"TERAPIA FISICA SESION\",\"quantity\":3,\"unitPrice\":1000.00," + clinicExempt,
            ["44"] = "{\"name\":\"SONOGRAFIA ABDOMINAL\",\"quantity\":1,\"unitPrice\":2500.00," + clinicExempt
                   + ",{\"name\":\"NEBULIZACION\",\"quantity\":2,\"unitPrice\":350.00," + clinicExempt,
            ["45"] = "{\"name\":\"VACUNACION Y ADMINISTRACION DE INYECTABLES\",\"quantity\":2,\"unitPrice\":400.00," + clinicExempt
                   + ",{\"name\":\"ELECTROCARDIOGRAMA 12 DERIVACIONES\",\"quantity\":1,\"unitPrice\":900.00," + clinicExempt,
        };

        // Renta de Vehículos (Id 15): mismas plantillas validadas de Transporte. Se temátican los
        // nombres de los ítems y se marcan como servicio (itemType 2). Las plantillas son exentas
        // (4), así que se pasan a gravado (1, ITBIS 18%) en los tipos 31, 32, 33, 34, 41 y 45
        // con "taxPercentage":18 explícito, como las muestras de Farmacia que la DGII acepta:
        // el simulador calcula el ITBIS con TaxPercentage, y sin él un ítem gravado sale con
        // TotalITBIS1 = 0 y la DGII lo rechaza. 43 (gastos menores), 44 (regímenes especiales),
        // 46 (exportación) y 47 (pagos al exterior) conservan el indicador de su plantilla.
        var rentalItemNames = new Dictionary<string, (string From, string To)>
        {
            ["31"] = ("CEDEBRAL 5000 JARABE", "ALQUILER KIA PICANTO 2025 (DIA)"),
            ["32"] = ("GREEN PIGEON PEAS CARIDOM 24/15 OZ.", "ALQUILER BUGGY CF MOTO UFORCE 600 (DIA)"),
            ["33"] = ("GENERAL", "AJUSTE POR DIAS DE ALQUILER"),
            ["34"] = ("CORACOR A C/30 TABS.", "CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE"),
            ["41"] = ("COMISION VERIFON TARJETAS", "MANTENIMIENTO Y LAVADO DE FLOTA"),
            ["43"] = ("PROPIETARIO COMPANIA DE TRANSPORTE DIVER", "REEMBOLSO CAJA CHICA COMBUSTIBLE"),
            ["44"] = ("GREEN PIGEON PEAS CARIDOM 24/15 OZ.", "ALQUILER SCOOTER TAURO DIO 110 (DIA)"),
            ["45"] = ("OXIGEN 200 C/30 TABS.", "ALQUILER SCOOTER SYM FNX 200CC (DIA)"),
            ["46"] = ("AGUACATE CRIOLLO", "ALQUILER DE VEHICULO A TURISTA EXTRANJERO"),
            ["47"] = ("SERVICIO PROFESIONAL EXTERIOR", "ALQUILER DE VEHICULO EN EL EXTERIOR"),
        };

        var rentalTaxedTypes = new HashSet<string> { "31", "32", "33", "34", "41", "45" };

        // Ítems adicionales después del ítem principal en los tipos con detalle libre
        // (31, 32, 44, 45). El tipo 44 usa ítems exentos; el resto, gravados con taxPercentage 18.
        const string rentalTaxed = "\"billingIndicator\":1,\"taxPercentage\":18,\"itemType\":2}";
        const string rentalExempt = "\"billingIndicator\":4,\"itemType\":2}";
        var rentalExtraItems = new Dictionary<string, string>
        {
            ["31"] = "{\"name\":\"ALQUILER NISSAN SENTRA SV (DIA)\",\"quantity\":1,\"unitPrice\":2100.00," + rentalTaxed
                   + ",{\"name\":\"ALQUILER SCOOTER CFMOTO 150CC (DIA)\",\"quantity\":1,\"unitPrice\":900.00," + rentalTaxed
                   + ",{\"name\":\"ENTREGA Y RECOGIDA A DOMICILIO\",\"quantity\":1,\"unitPrice\":500.00," + rentalTaxed,
            ["32"] = "{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00," + rentalTaxed
                   + ",{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00," + rentalTaxed,
            ["44"] = "{\"name\":\"ALQUILER SYM FNX 200CC (DIA)\",\"quantity\":1,\"unitPrice\":900.00," + rentalExempt
                   + ",{\"name\":\"ENTREGA Y RECOGIDA A DOMICILIO\",\"quantity\":1,\"unitPrice\":500.00," + rentalExempt,
            ["45"] = "{\"name\":\"ALQUILER SCOOTER SUZUKI AN 125 (DIA)\",\"quantity\":1,\"unitPrice\":600.00," + rentalTaxed
                   + ",{\"name\":\"CASCO Y ACCESORIOS\",\"quantity\":1,\"unitPrice\":300.00," + rentalTaxed,
        };

        var nextId = samples.Max(s => s.BusinessSimulationSampleId) + 1;

        foreach (var businessType in businessTypes.Where(b => b.BusinessTypeId != 1))
        {
            foreach (var ecfType in requiredTypes)
            {
                if (existingPairs.Contains((businessType.BusinessTypeId, ecfType))) continue;
                if (!templatesByType.TryGetValue(ecfType, out var json)) continue;

                if (businessType.BusinessTypeId == 14 && clinicItemNames.TryGetValue(ecfType, out var clinicName))
                {
                    // Ítem principal tematizado y marcado explícitamente como servicio (itemType 2).
                    json = json.Replace($"\"name\":\"{clinicName.From}\"", $"\"name\":\"{clinicName.To}\",\"itemType\":2");

                    if (clinicExtraItems.TryGetValue(ecfType, out var extraItems))
                    {
                        // Inserta los servicios adicionales justo después del primer ítem.
                        var firstItemEnd = json.IndexOf('}', json.IndexOf("\"items\":[", StringComparison.Ordinal));
                        json = json.Insert(firstItemEnd + 1, "," + extraItems);
                    }
                }

                if (businessType.BusinessTypeId == 15 && rentalItemNames.TryGetValue(ecfType, out var rentalName))
                {
                    // Ítem principal tematizado y marcado como servicio (itemType 2).
                    json = json.Replace($"\"name\":\"{rentalName.From}\"", $"\"name\":\"{rentalName.To}\",\"itemType\":2");

                    // La plantilla es exenta (4); se pasa a gravado con taxPercentage 18 explícito
                    // antes de insertar los extras, que ya traen su propio indicador.
                    if (rentalTaxedTypes.Contains(ecfType))
                        json = json.Replace("\"billingIndicator\":4", "\"billingIndicator\":1,\"taxPercentage\":18");

                    if (rentalExtraItems.TryGetValue(ecfType, out var rentalExtra))
                    {
                        // Inserta los ítems adicionales justo después del primer ítem.
                        var firstItemEnd = json.IndexOf('}', json.IndexOf("\"items\":[", StringComparison.Ordinal));
                        json = json.Insert(firstItemEnd + 1, "," + rentalExtra);
                    }
                }

                samples.Add(new BusinessSimulationSample
                {
                    BusinessSimulationSampleId = nextId,
                    BusinessTypeId = businessType.BusinessTypeId,
                    EcfType = ecfType,
                    Name = $"{businessType.Name} - Tipo {ecfType}",
                    Description = $"Muestra generada (plantilla validada) para Tipo {ecfType}.",
                    // GuidId determinista y único por (negocio, tipo).
                    GuidId = $"00000000-0000-0000-{businessType.BusinessTypeId:D4}-0000000000{ecfType}",
                    RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                    JsonData = json
                });
                existingPairs.Add((businessType.BusinessTypeId, ecfType));
                nextId++;
            }
        }

        modelBuilder.Entity<BusinessSimulationSample>().HasData(samples);
    }
}
