using System;
using System.Collections.Generic;
using System.Text.Json;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Certification;

public static class SimulationSampleGenerator
{
    public static string GenerateJson(string businessType, string ecfType, string rnc = "133009889", decimal? forceAmount = null)
    {
        var items = businessType switch
        {
            "Farmacia" => new List<EcfItemRequestDto>
            {
                new() { NombreItem = "Amoxicilina 500mg (Caja 30)", CantidadItem = 1, PrecioUnitarioItem = 850, MontoItem = 850, IndicadorFacturacion = "0" }, // Exento
                new() { NombreItem = "Vitaminas Complejo B", CantidadItem = 2, PrecioUnitarioItem = 450, MontoItem = 900, IndicadorFacturacion = "0" },
                new() { NombreItem = "Jabón Antibacterial", CantidadItem = 3, PrecioUnitarioItem = 125, MontoItem = 375, IndicadorFacturacion = "1" } // Gravado
            },
            "Repuesto" => new List<EcfItemRequestDto>
            {
                new() { NombreItem = "Pastillas de Freno Delanteras", CantidadItem = 1, PrecioUnitarioItem = 2500, MontoItem = 2500, IndicadorFacturacion = "1" },
                new() { NombreItem = "Filtro de Aire Motor", CantidadItem = 1, PrecioUnitarioItem = 950, MontoItem = 950, IndicadorFacturacion = "1" },
                new() { NombreItem = "Aceite Sintético 5W-30 (Cuarto)", CantidadItem = 5, PrecioUnitarioItem = 750, MontoItem = 3750, IndicadorFacturacion = "1" }
            },
            "Taller de Mecánica" => new List<EcfItemRequestDto>
            {
                new() { NombreItem = "Mano de Obra Mantenimiento Preventivo", CantidadItem = 1, PrecioUnitarioItem = 3500, MontoItem = 3500, IndicadorFacturacion = "2" }, // Servicio
                new() { NombreItem = "Alineación y Balanceo Computarizado", CantidadItem = 1, PrecioUnitarioItem = 1800, MontoItem = 1800, IndicadorFacturacion = "2" },
                new() { NombreItem = "Limpieza de Inyectores", CantidadItem = 1, PrecioUnitarioItem = 2200, MontoItem = 2200, IndicadorFacturacion = "2" }
            },
            "Surtidora" => new List<EcfItemRequestDto>
            {
                new() { NombreItem = "Arroz Premium (Saco 50lb)", CantidadItem = 10, PrecioUnitarioItem = 1850, MontoItem = 18500, IndicadorFacturacion = "0" },
                new() { NombreItem = "Aceite Vegetal (Caja 12 unidades)", CantidadItem = 5, PrecioUnitarioItem = 2400, MontoItem = 12000, IndicadorFacturacion = "0" },
                new() { NombreItem = "Galletas Dulces (Caja 24/12)", CantidadItem = 2, PrecioUnitarioItem = 1150, MontoItem = 2300, IndicadorFacturacion = "1" }
            },
            "Librerías" => new List<EcfItemRequestDto>
            {
                new() { NombreItem = "Libro: Cien Años de Soledad", CantidadItem = 1, PrecioUnitarioItem = 950, MontoItem = 950, IndicadorFacturacion = "0" }, // Libros exentos
                new() { NombreItem = "Cuaderno Espiral 200 pág.", CantidadItem = 12, PrecioUnitarioItem = 145, MontoItem = 1740, IndicadorFacturacion = "1" },
                new() { NombreItem = "Mochila Ergonómica Escolar", CantidadItem = 1, PrecioUnitarioItem = 3200, MontoItem = 3200, IndicadorFacturacion = "1" }
            },
            "Transporte" => new List<EcfItemRequestDto>
            {
                new() { NombreItem = "Servicio de Flete Local (Santo Domingo)", CantidadItem = 1, PrecioUnitarioItem = 4500, MontoItem = 4500, IndicadorFacturacion = "2" },
                new() { NombreItem = "Transporte de Carga Interurbana", CantidadItem = 1, PrecioUnitarioItem = 12500, MontoItem = 12500, IndicadorFacturacion = "2" },
                new() { NombreItem = "Seguro de Carga", CantidadItem = 1, PrecioUnitarioItem = 1500, MontoItem = 1500, IndicadorFacturacion = "2" }
            },
            _ => new List<EcfItemRequestDto> { new() { NombreItem = "Venta de Mercancía General", CantidadItem = 1, PrecioUnitarioItem = 1000, MontoItem = 1000, IndicadorFacturacion = "1" } }
        };

        if (forceAmount.HasValue)
        {
            // Simple adjustment to reach the forced total (ignoring ITBIS for simplicity in the forced calculation)
            items[0].PrecioUnitarioItem = forceAmount.Value;
            items[0].MontoItem = forceAmount.Value;
            items.RemoveRange(1, items.Count - 1);
        }

        decimal montoGravado = 0;
        decimal montoExento = 0;
        foreach (var item in items)
        {
            if (item.IndicadorFacturacion == "1" || item.IndicadorFacturacion == "2")
                montoGravado += item.MontoItem;
            else
                montoExento += item.MontoItem;
        }

        decimal itbis = Math.Round(montoGravado * 0.18m, 2);
        decimal total = montoExento + montoGravado + itbis;

        var request = new EcfInvoiceRequestDto
        {
            ECF = new EcfRequest
            {
                Encabezado = new EcfEncabezadoRequest
                {
                    IdDoc = new EcfIdDocRequest
                    {
                        TipoeCF = ecfType,
                        eNCF = $"E{ecfType}0000000001",
                        FechaVencimientoSecuencia = "2026-12-31"
                    },
                    Emisor = new EcfEmisorRequest
                    {
                        RNCEmisor = rnc,
                        RazonSocialEmisor = $"{businessType.ToUpper()} PROFESIONAL TEST SRL",
                        DireccionEmisor = "AV. 27 DE FEBRERO #456, SANTO DOMINGO",
                        FechaEmision = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                    },
                    Comprador = new EcfCompradorRequest
                    {
                        RNCComprador = (ecfType == "46" || ecfType == "32") ? "" : "101010101", 
                        RazonSocialComprador = ecfType == "46" ? "INTERNATIONAL BUYER CORP" : (ecfType == "32" ? "CLIENTE FINAL" : "CLIENTE DE PRUEBA CERTIFICACION"),
                        DireccionComprador = ecfType == "46" ? "MIAMI, FL, USA" : "AV. WINSTON CHURCHILL, SDQ"
                    },
                    Totales = new EcfTotalesRequest
                    {
                        MontoGravadoTotal = montoGravado,
                        MontoGravadoI1 = montoGravado,
                        TotalITBIS = itbis,
                        TotalITBIS1 = itbis,
                        MontoExento = montoExento > 0 ? montoExento : null,
                        MontoTotal = total
                    }
                },
                DetallesItems = new EcfDetallesItemsRequest { Item = items }
            }
        };

        if (ecfType == "46") // Export
        {
            request.ECF.Encabezado.Comprador.IdentificadorExtranjero = "ID12345678";
        }

        return JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
    }
}
