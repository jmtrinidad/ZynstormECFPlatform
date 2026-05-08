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
            new BusinessType { BusinessTypeId = 11, Name = "Cafeteria", Description = "Venta de café, postres y comidas ligeras.", GuidId = "7a8b9c0d-1e2f-3g4h-5i6j-7k8l9m0n1o2p", RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z") }
        };

        modelBuilder.Entity<BusinessType>().HasData(businessTypes);

        var samples = new List<BusinessSimulationSample>
        {
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 1, 
                BusinessTypeId = 1, 
                EcfType = "31", 
                GuidId = "98765432-1234-5678-90ab-cdef12345601",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"issuerRnc\":\"133009889\",\"issuerName\":\"TRANSPORTE NJ, SRL\",\"issuerAddress\":\"Ensanche Gregorio Luperon, Santiago\",\"customerRnc\":\"102620717\",\"customerName\":\"MORTEROS DE EUROPA\",\"incomeType\":\"01\",\"paymentType\":2,\"manualIndicadorMontoGravado\":0,\"items\":[{\"name\":\"Servicio de Transporte de Carga\",\"quantity\":1,\"unitPrice\":6000.00,\"taxPercentage\":0,\"billingIndicator\":4}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 2, 
                BusinessTypeId = 2, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345602",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Farmacia Salud\",\"IssuerAddress\":\"Av. 27 de Febrero 123\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Amoxicilina 500mg\",\"Quantity\":1,\"UnitPrice\":450.00,\"TaxPercentage\":0},{\"Name\":\"Vitamina C 1000mg\",\"Quantity\":2,\"UnitPrice\":300.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 3, 
                BusinessTypeId = 3, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345603",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Repuestos El Motor\",\"IssuerAddress\":\"Calle Duarte 45\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Filtro de Aceite\",\"Quantity\":1,\"UnitPrice\":650.00,\"TaxPercentage\":18},{\"Name\":\"Aceite Sintético 5W30\",\"Quantity\":4,\"UnitPrice\":850.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 4, 
                BusinessTypeId = 4, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345604",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Taller Los Amigos\",\"IssuerAddress\":\"Av. Imbert 78\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cambio de Aceite (Labor)\",\"Quantity\":1,\"UnitPrice\":1500.00,\"TaxPercentage\":18},{\"Name\":\"Revisión de Frenos\",\"Quantity\":1,\"UnitPrice\":800.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 5, 
                BusinessTypeId = 5, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345605",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Surtidora El Pueblo\",\"IssuerAddress\":\"Calle Central 10\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Refresco 2L\",\"Quantity\":12,\"UnitPrice\":75.00,\"TaxPercentage\":18},{\"Name\":\"Arroz 10lb\",\"Quantity\":5,\"UnitPrice\":350.00,\"TaxPercentage\":0}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 6, 
                BusinessTypeId = 6, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345606",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Librería Minerva\",\"IssuerAddress\":\"Calle Independencia 55\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cuaderno A4\",\"Quantity\":5,\"UnitPrice\":120.00,\"TaxPercentage\":18},{\"Name\":\"Lápiz de Grafito\",\"Quantity\":20,\"UnitPrice\":15.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 7, 
                BusinessTypeId = 7, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345671",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Pinturas del Este\",\"IssuerAddress\":\"Av. Independencia 456\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cubeta Pintura Blanca Satinada\",\"Quantity\":1,\"UnitPrice\":2000.00,\"TaxPercentage\":18},{\"Name\":\"Brocha 4 Pulgadas Profesional\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 8, 
                BusinessTypeId = 8, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345672",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Boutique Elegance\",\"IssuerAddress\":\"Calle del Sol 789\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Vestido de Gala Azul\",\"Quantity\":1,\"UnitPrice\":3500.00,\"TaxPercentage\":18},{\"Name\":\"Cinturón Cuero Genuino\",\"Quantity\":1,\"UnitPrice\":1000.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 9, 
                BusinessTypeId = 9, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345673",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Colchones Confort\",\"IssuerAddress\":\"Av. Winston Churchill 101\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Colchón King Size Ortopédico\",\"Quantity\":1,\"UnitPrice\":12000.00,\"TaxPercentage\":18},{\"Name\":\"Almohada Memory Foam\",\"Quantity\":2,\"UnitPrice\":1500.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 10, 
                BusinessTypeId = 10, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345674",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Restaurante Sabores\",\"IssuerAddress\":\"Calle Gourmet 202\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Cena Especial del Chef (Dúo)\",\"Quantity\":1,\"UnitPrice\":2500.00,\"TaxPercentage\":18},{\"Name\":\"Botella de Vino Tinto Reserva\",\"Quantity\":1,\"UnitPrice\":700.00,\"TaxPercentage\":18}]}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 11, 
                BusinessTypeId = 11, 
                EcfType = "32", 
                GuidId = "98765432-1234-5678-90ab-cdef12345675",
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"IssuerRnc\":\"131794021\",\"IssuerName\":\"Café Aroma\",\"IssuerAddress\":\"Plaza Central Local 5\",\"CustomerRnc\":\"22400000000\",\"CustomerName\":\"Consumidor Final\",\"Items\":[{\"Name\":\"Café Latte Grande\",\"Quantity\":2,\"UnitPrice\":175.00,\"TaxPercentage\":18},{\"Name\":\"Croissant de Almendras\",\"Quantity\":2,\"UnitPrice\":250.00,\"TaxPercentage\":18}]}"
            }
        };

        modelBuilder.Entity<BusinessSimulationSample>().HasData(samples);
    }
}
