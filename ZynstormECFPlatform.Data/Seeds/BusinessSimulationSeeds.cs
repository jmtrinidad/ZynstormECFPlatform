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
                BusinessTypeId = 7, 
                EcfType = "32", 
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000001\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Pinturas del Este\",\"DireccionEmisor\":\"Av. Independencia 456\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":2500.00,\"MontoGravadoI1\":2500.00,\"ITBIS1\":18,\"TotalITBIS\":450.00,\"TotalITBIS1\":450.00,\"MontoTotal\":2950.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Cubeta Pintura Blanca Satinada\",\"CantidadItem\":1,\"PrecioUnitarioItem\":2000.00,\"MontoItem\":2000.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Brocha 4 Pulgadas Profesional\",\"CantidadItem\":2,\"PrecioUnitarioItem\":250.00,\"MontoItem\":500.00}]}}}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 2, 
                BusinessTypeId = 8, 
                EcfType = "32", 
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000002\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Boutique Elegance\",\"DireccionEmisor\":\"Calle del Sol 789\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":4500.00,\"MontoGravadoI1\":4500.00,\"ITBIS1\":18,\"TotalITBIS\":810.00,\"TotalITBIS1\":810.00,\"MontoTotal\":5310.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Vestido de Gala Azul\",\"CantidadItem\":1,\"PrecioUnitarioItem\":3500.00,\"MontoItem\":3500.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Cinturón Cuero Genuino\",\"CantidadItem\":1,\"PrecioUnitarioItem\":1000.00,\"MontoItem\":1000.00}]}}}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 3, 
                BusinessTypeId = 9, 
                EcfType = "32", 
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000003\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Colchones Confort\",\"DireccionEmisor\":\"Av. Winston Churchill 101\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":15000.00,\"MontoGravadoI1\":15000.00,\"ITBIS1\":18,\"TotalITBIS\":2700.00,\"TotalITBIS1\":2700.00,\"MontoTotal\":17700.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Colchón King Size Ortopédico\",\"CantidadItem\":1,\"PrecioUnitarioItem\":12000.00,\"MontoItem\":12000.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Almohada Memory Foam\",\"CantidadItem\":2,\"PrecioUnitarioItem\":1500.00,\"MontoItem\":3000.00}]}}}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 4, 
                BusinessTypeId = 10, 
                EcfType = "32", 
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000004\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Restaurante Sabores\",\"DireccionEmisor\":\"Calle Gourmet 202\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":3200.00,\"MontoGravadoI1\":3200.00,\"ITBIS1\":18,\"TotalITBIS\":576.00,\"TotalITBIS1\":576.00,\"MontoTotal\":3776.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Cena Especial del Chef (Dúo)\",\"CantidadItem\":1,\"PrecioUnitarioItem\":2500.00,\"MontoItem\":2500.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Botella de Vino Tinto Reserva\",\"CantidadItem\":1,\"PrecioUnitarioItem\":700.00,\"MontoItem\":700.00}]}}}"
            },
            new BusinessSimulationSample 
            { 
                BusinessSimulationSampleId = 5, 
                BusinessTypeId = 11, 
                EcfType = "32", 
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTime.Parse("2026-05-07T00:00:00Z"),
                JsonData = "{\"ECF\":{\"Encabezado\":{\"Version\":\"1.0\",\"IdDoc\":{\"TipoeCF\":\"32\",\"eNCF\":\"E320000000005\",\"FechaVencimientoSecuencia\":\"2026-12-31\"},\"Emisor\":{\"RNCEmisor\":\"131794021\",\"RazonSocialEmisor\":\"Café Aroma\",\"DireccionEmisor\":\"Plaza Central Local 5\",\"FechaEmision\":\"2026-05-07\"},\"Comprador\":{\"RazonSocialComprador\":\"Consumidor Final\"},\"Totales\":{\"MontoGravadoTotal\":850.00,\"MontoGravadoI1\":850.00,\"ITBIS1\":18,\"TotalITBIS\":153.00,\"TotalITBIS1\":153.00,\"MontoTotal\":1003.00}},\"DetallesItems\":{\"Item\":[{\"NumeroLinea\":\"1\",\"NombreItem\":\"Café Latte Grande\",\"CantidadItem\":2,\"PrecioUnitarioItem\":175.00,\"MontoItem\":350.00},{\"NumeroLinea\":\"2\",\"NombreItem\":\"Croissant de Almendras\",\"CantidadItem\":2,\"PrecioUnitarioItem\":250.00,\"MontoItem\":500.00}]}}}"
            }
        };

        modelBuilder.Entity<BusinessSimulationSample>().HasData(samples);
    }
}
