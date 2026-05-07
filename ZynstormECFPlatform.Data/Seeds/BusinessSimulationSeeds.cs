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
            new BusinessType { BusinessTypeId = 6, Name = "Librerías", Description = "Venta de libros, útiles escolares y papelería.", GuidId = "2a3b4c5d-6e7f-8g9h-0i1j-2k3l4m5n6o7p", RegisteredAt = DateTime.Parse("2026-05-06T00:00:00Z") }
        };

        modelBuilder.Entity<BusinessType>().HasData(businessTypes);
    }
}
