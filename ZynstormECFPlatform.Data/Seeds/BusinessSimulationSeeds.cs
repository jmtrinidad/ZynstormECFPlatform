using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            new BusinessType { BusinessTypeId = 13, Name = "Mueblería", Description = "Venta de muebles, decoración y artículos para el hogar.", GuidId = "9b0c1d2e-3f4g-5h6i-7j8k-9l0m1n2o3p4r", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z") }
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
                JsonData = "{\"ncf\":\"E410000000001\",\"customerRnc\":\"00100325067\",\"customerName\":\"ENRIQUE CAMILO SANTOS TAVAREZ\",\"incomeType\":\"01\",\"paymentType\":2,\"items\":[{\"name\":\"COMISION VERIFON TARJETAS\",\"quantity\":1,\"unitPrice\":1000.00,\"billingIndicator\":1,\"taxPercentage\":18}]}"
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
                JsonData = "{\"ncf\":\"E310000000001\",\"customerRnc\":\"130862346\",\"customerName\":\"IT SOLUCLICK SRL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"NEVERA SAMSUNG BESPOKE 23 P3\",\"quantity\":1,\"unitPrice\":85000.00,\"billingIndicator\":1},{\"name\":\"TELEVISOR LG OLED 55\\\"\",\"quantity\":1,\"unitPrice\":65000.00,\"billingIndicator\":1}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 22, BusinessTypeId = 12, EcfType = "32", 
                Name = "Tienda Electrodomésticos Consumo", Description = "Ejemplo validado para Tipo 32.",
                GuidId = "98765432-1234-5678-90ab-cdef12345622", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"LICUADORA NINJA PROFESSIONAL\",\"quantity\":1,\"unitPrice\":8500.00,\"billingIndicator\":1},{\"name\":\"FREIDORA DE AIRE DIGITAL 5.5L\",\"quantity\":1,\"unitPrice\":7200.00,\"billingIndicator\":1}]}"
            },
            // --- MUEBLERÍA ---
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 23, BusinessTypeId = 13, EcfType = "31", 
                Name = "Mueblería Crédito Fiscal", Description = "Ejemplo validado para Tipo 31.",
                GuidId = "98765432-1234-5678-90ab-cdef12345623", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E310000000001\",\"customerRnc\":\"131880657\",\"customerName\":\"MUEBLES Y DECORACIONES S.A.\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"JUEGO DE COMEDOR MODERNO 6 SILLAS\",\"quantity\":1,\"unitPrice\":45000.00,\"billingIndicator\":1}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 24, BusinessTypeId = 13, EcfType = "32", 
                Name = "Mueblería Consumo", Description = "Ejemplo validado para Tipo 32.",
                GuidId = "98765432-1234-5678-90ab-cdef12345624", RegisteredAt = DateTime.Parse("2026-05-08T00:00:00Z"),
                JsonData = "{\"ncf\":\"E320000000001\",\"customerRnc\":\"22400000000\",\"customerName\":\"CONSUMIDOR FINAL\",\"incomeType\":\"01\",\"paymentType\":1,\"items\":[{\"name\":\"SOFA SECCIONAL EN TELA GRIS\",\"quantity\":1,\"unitPrice\":38000.00,\"billingIndicator\":1},{\"name\":\"CAMA QUEEN SIZE CON BASE\",\"quantity\":1,\"unitPrice\":22000.00,\"billingIndicator\":1}]}"
            }
        };

        modelBuilder.Entity<BusinessSimulationSample>().HasData(samples);
    }
}
