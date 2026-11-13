# Renta de Vehículos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Agregar el tipo de negocio "Renta de Vehículos" (Id 15) con sus 10 muestras de simulación e-CF al selector de la simulación de certificación.

**Architecture:** Mismo mecanismo que Clínica/Servicio Ambulatorio: el `BusinessType` y las muestras salen de `BusinessSimulationSeeds`, que parte de las plantillas validadas de Transporte y sustituye nombres de ítems e inserta ítems de servicio adicionales. `SimulationSampleGenerator` recibe un caso equivalente para la ruta de regeneración, y una migración EF inserta los datos.

**Tech Stack:** .NET 10, EF Core 10 (Npgsql), xUnit, `dotnet ef`.

**Spec:** `docs/superpowers/specs/2026-11-13-renta-vehiculos-design.md`

## Global Constraints

- Rama de trabajo: `dev`. Verificar `git branch --show-current` en el mismo comando de cada commit.
- No hacer `git push`, no replicar a `Staging`, no aplicar la migración a ninguna base de datos.
- `BusinessType`: `BusinessTypeId = 15`, `Name = "Renta de Vehículos"`, `Description = "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%)."`, `GuidId = "1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a"`, `RegisteredAt = 2026-11-13`.
- Una muestra por tipo de e-CF (31, 32, 33, 34, 41, 43, 44, 45, 46, 47), ids correlativos desde `max(id) + 1` (142 a 151), `GuidId = 00000000-0000-0000-0015-0000000000{ecfType}`, `Name = "Renta de Vehículos - Tipo {ecfType}"`. Esto lo produce el bucle existente del seed, sin tocarlo.
- Todos los ítems de las muestras llevan `itemType` 2 (servicio).
- Gravados con ITBIS 18% (`billingIndicator` 1): tipos 31, 32, 33, 34, 41 y 45. Conservan el indicador de su plantilla: 43 (4), 44 (4), 46 (3) y 47 (4). Los ítems adicionales del tipo 44 son exentos (4).
- Se conservan los montos, RNC y demás campos de la plantilla de Transporte. Los tipos 33, 34, 41, 43, 46 y 47 mantienen un solo ítem.
- Precios de los ítems adicionales en RD$: Nissan Sentra 2,100; scooter CFMOTO 150cc 900; scooter SYM FNX 200cc 900; scooter Suzuki AN 125 600; entrega y recogida 500; casco y accesorios 300 (cantidad 1 cada uno).
- Mensajes de commit en español, terminados con `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Comandos desde `C:/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform`.
- La prueba `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada` ya fallaba antes de este trabajo y no está relacionada; el resto de la suite debe pasar.

---

### Task 1: Tipo de negocio y muestras en el seed

**Files:**
- Create: `ZynstormECFPlatform.Tests/Certification/RentaVehiculosSeedTests.cs`
- Modify: `ZynstormECFPlatform.Data/Seeds/BusinessSimulationSeeds.cs` (línea 28 y bloque antes de `var nextId`, y el bucle `foreach`)

**Interfaces:**
- Consumes: `BusinessSimulationSeeds.Seed(ModelBuilder)` ya existente, llamado desde `StorageContext.OnModelCreating`.
- Produces: en el modelo de diseño, un `BusinessType` con `BusinessTypeId = 15` y 10 `BusinessSimulationSample` con ese `BusinessTypeId`. La Task 3 genera la migración a partir de esto.

- [ ] **Step 1: Escribir la prueba que falla**

Crear `ZynstormECFPlatform.Tests/Certification/RentaVehiculosSeedTests.cs`:

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;

namespace ZynstormECFPlatform.Tests.Certification;

public class RentaVehiculosSeedTests
{
    private const int RentaId = 15;
    private const int ClinicaId = 14;

    private static readonly string[] EcfTypes = ["31", "32", "33", "34", "41", "43", "44", "45", "46", "47"];

    // Construir el modelo no abre conexión: la cadena solo selecciona el proveedor Npgsql.
    private static readonly IModel Model = BuildModel();

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<StorageContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=x;Password=x")
            .Options;
        using var context = new StorageContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static List<IDictionary<string, object?>> AllSamples() =>
        Model.FindEntityType(typeof(BusinessSimulationSample))!.GetSeedData().ToList();

    private static List<IDictionary<string, object?>> SamplesOf(int businessTypeId) =>
        AllSamples().Where(d => (int)d["BusinessTypeId"]! == businessTypeId).ToList();

    private static IDictionary<string, object?> RentaSample(string ecfType) =>
        SamplesOf(RentaId).Single(d => (string)d["EcfType"]! == ecfType);

    private record Item(string Name, decimal Quantity, decimal UnitPrice, int BillingIndicator, int ItemType);

    private static List<Item> ItemsOf(string ecfType)
    {
        using var doc = JsonDocument.Parse((string)RentaSample(ecfType)["JsonData"]!);
        return doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(e => new Item(
                e.GetProperty("name").GetString()!,
                e.GetProperty("quantity").GetDecimal(),
                e.GetProperty("unitPrice").GetDecimal(),
                e.GetProperty("billingIndicator").GetInt32(),
                e.GetProperty("itemType").GetInt32()))
            .ToList();
    }

    [Fact]
    public void BusinessType15_IsRentaDeVehiculos()
    {
        var type = Model.FindEntityType(typeof(BusinessType))!.GetSeedData()
            .Single(d => (int)d["BusinessTypeId"]! == RentaId);

        Assert.Equal("Renta de Vehículos", type["Name"]);
        Assert.Equal("1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a", type["GuidId"]);
        Assert.Equal(
            "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%).",
            type["Description"]);
    }

    [Fact]
    public void Renta_HasOneSamplePerEcfType_WithConsecutiveIds()
    {
        var samples = SamplesOf(RentaId);

        Assert.Equal(EcfTypes.OrderBy(t => t), samples.Select(s => (string)s["EcfType"]!).OrderBy(t => t));
        Assert.Equal(Enumerable.Range(142, 10), samples.Select(s => (int)s["BusinessSimulationSampleId"]!).OrderBy(i => i));
        Assert.All(samples, s =>
        {
            var ecf = (string)s["EcfType"]!;
            Assert.Equal($"Renta de Vehículos - Tipo {ecf}", s["Name"]);
            Assert.Equal($"00000000-0000-0000-0015-0000000000{ecf}", s["GuidId"]);
        });
    }

    [Fact]
    public void Clinica_KeepsItsSampleIds()
    {
        var ids = SamplesOf(ClinicaId).Select(s => (int)s["BusinessSimulationSampleId"]!).OrderBy(i => i);
        Assert.Equal(Enumerable.Range(132, 10), ids);
    }

    [Fact]
    public void AllSampleGuidIds_AreUnique()
    {
        var guids = AllSamples().Select(s => (string)s["GuidId"]!).ToList();
        Assert.Equal(guids.Count, guids.Distinct().Count());
    }

    [Theory]
    [InlineData("31", "ALQUILER KIA PICANTO 2025 (DIA)")]
    [InlineData("32", "ALQUILER BUGGY CF MOTO UFORCE 600 (DIA)")]
    [InlineData("33", "AJUSTE POR DIAS DE ALQUILER")]
    [InlineData("34", "CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE")]
    [InlineData("41", "MANTENIMIENTO Y LAVADO DE FLOTA")]
    [InlineData("43", "REEMBOLSO CAJA CHICA COMBUSTIBLE")]
    [InlineData("44", "ALQUILER SCOOTER TAURO DIO 110 (DIA)")]
    [InlineData("45", "ALQUILER SCOOTER SYM FNX 200CC (DIA)")]
    [InlineData("46", "ALQUILER DE VEHICULO A TURISTA EXTRANJERO")]
    [InlineData("47", "ALQUILER DE VEHICULO EN EL EXTERIOR")]
    public void MainItem_IsThemedRentalService(string ecfType, string expectedName)
    {
        var main = ItemsOf(ecfType)[0];
        Assert.Equal(expectedName, main.Name);
        Assert.Equal(2, main.ItemType);
    }

    [Theory]
    [InlineData("31", 4)]
    [InlineData("32", 3)]
    [InlineData("33", 1)]
    [InlineData("34", 1)]
    [InlineData("41", 1)]
    [InlineData("43", 1)]
    [InlineData("44", 3)]
    [InlineData("45", 3)]
    [InlineData("46", 1)]
    [InlineData("47", 1)]
    public void ItemCount_MatchesSpec(string ecfType, int expectedCount)
    {
        Assert.Equal(expectedCount, ItemsOf(ecfType).Count);
    }

    [Theory]
    [InlineData("31", 1)]
    [InlineData("32", 1)]
    [InlineData("33", 1)]
    [InlineData("34", 1)]
    [InlineData("41", 1)]
    [InlineData("45", 1)]
    [InlineData("43", 4)]
    [InlineData("44", 4)]
    [InlineData("46", 3)]
    [InlineData("47", 4)]
    public void BillingIndicator_FollowsTaxRule(string ecfType, int expectedIndicator)
    {
        Assert.All(ItemsOf(ecfType), i => Assert.Equal(expectedIndicator, i.BillingIndicator));
    }

    [Fact]
    public void Type31_HasExtraRentalItemsWithReferencePrices()
    {
        var items = ItemsOf("31");

        Assert.Equal(
            new (string, decimal)[]
            {
                ("ALQUILER KIA PICANTO 2025 (DIA)", 244.00m),
                ("ALQUILER NISSAN SENTRA SV (DIA)", 2100.00m),
                ("ALQUILER SCOOTER CFMOTO 150CC (DIA)", 900.00m),
                ("ENTREGA Y RECOGIDA A DOMICILIO", 500.00m),
            },
            items.Select(i => (i.Name, i.UnitPrice)).ToArray());
        Assert.All(items, i => Assert.Equal(2, i.ItemType));
    }

    [Fact]
    public void Type44_ExtraItemsAreExemptServices()
    {
        var items = ItemsOf("44");

        Assert.Equal(
            new (string, decimal)[]
            {
                ("ALQUILER SCOOTER TAURO DIO 110 (DIA)", 29.50m),
                ("ALQUILER SYM FNX 200CC (DIA)", 900.00m),
                ("ENTREGA Y RECOGIDA A DOMICILIO", 500.00m),
            },
            items.Select(i => (i.Name, i.UnitPrice)).ToArray());
        Assert.All(items, i =>
        {
            Assert.Equal(4, i.BillingIndicator);
            Assert.Equal(2, i.ItemType);
        });
    }
}
```

- [ ] **Step 2: Ejecutar la prueba y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter RentaVehiculosSeedTests 2>&1 | grep -E " error |Passed!|Failed!"`
Expected: `Failed!` con fallos como `Sequence contains no matching element` (el tipo 15 todavía no existe). No debe haber errores de compilación.

- [ ] **Step 3: Agregar el `BusinessType` 15**

En `ZynstormECFPlatform.Data/Seeds/BusinessSimulationSeeds.cs`, reemplazar la línea del tipo 14 (agregar coma y la línea nueva):

Reemplazar:
```csharp
            new BusinessType { BusinessTypeId = 14, Name = "Clínica/Servicio Ambulatorio", Description = "Consultas, procedimientos y servicios de salud ambulatorios (mayormente exentos de ITBIS).", GuidId = "0c1d2e3f-4a5b-6c7d-8e9f-0a1b2c3d4e5f", RegisteredAt = DateTime.Parse("2026-11-03T00:00:00Z") }
        };
```
por:
```csharp
            new BusinessType { BusinessTypeId = 14, Name = "Clínica/Servicio Ambulatorio", Description = "Consultas, procedimientos y servicios de salud ambulatorios (mayormente exentos de ITBIS).", GuidId = "0c1d2e3f-4a5b-6c7d-8e9f-0a1b2c3d4e5f", RegisteredAt = DateTime.Parse("2026-11-03T00:00:00Z") },
            new BusinessType { BusinessTypeId = 15, Name = "Renta de Vehículos", Description = "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%).", GuidId = "1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a", RegisteredAt = DateTime.Parse("2026-11-13T00:00:00Z") }
        };
```

- [ ] **Step 4: Agregar los mapas de nombres e ítems de Renta**

En el mismo archivo, insertar este bloque inmediatamente antes de la línea `var nextId = samples.Max(s => s.BusinessSimulationSampleId) + 1;`:

```csharp
        // Renta de Vehículos (Id 15): mismas plantillas validadas de Transporte. Se temátican
        // los nombres de los ítems y se marcan como servicio (itemType 2). Las plantillas son
        // exentas (4), así que se pasan a gravado (1, ITBIS 18%) solo en los tipos donde la
        // norma lo permite; 43 (gastos menores), 44 (regímenes especiales), 46 (exportación)
        // y 47 (pagos al exterior) conservan el indicador de su plantilla.
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
        // (31, 32, 44, 45). El tipo 44 usa ítems exentos; el resto, gravados.
        const string rentalTaxed = "\"billingIndicator\":1,\"itemType\":2}";
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

```

- [ ] **Step 5: Aplicar los mapas dentro del bucle**

En el bucle, justo después del bloque `if (businessType.BusinessTypeId == 14 && ...) { ... }` y antes de `samples.Add(new BusinessSimulationSample`, insertar:

```csharp
                if (businessType.BusinessTypeId == 15 && rentalItemNames.TryGetValue(ecfType, out var rentalName))
                {
                    // Ítem principal tematizado y marcado como servicio (itemType 2).
                    json = json.Replace($"\"name\":\"{rentalName.From}\"", $"\"name\":\"{rentalName.To}\",\"itemType\":2");

                    // La plantilla es exenta (4); se pasa a gravado antes de insertar los extras,
                    // que ya traen su propio indicador.
                    if (rentalTaxedTypes.Contains(ecfType))
                        json = json.Replace("\"billingIndicator\":4", "\"billingIndicator\":1");

                    if (rentalExtraItems.TryGetValue(ecfType, out var rentalExtra))
                    {
                        // Inserta los ítems adicionales justo después del primer ítem.
                        var firstItemEnd = json.IndexOf('}', json.IndexOf("\"items\":[", StringComparison.Ordinal));
                        json = json.Insert(firstItemEnd + 1, "," + rentalExtra);
                    }
                }

```

- [ ] **Step 6: Ejecutar las pruebas y comprobar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests --filter RentaVehiculosSeedTests 2>&1 | grep -E " error |Passed!|Failed!|Failed "`
Expected: `Passed!  - Failed: 0, Passed: 36` (4 hechos básicos, 3 teorías de 10 casos y 2 hechos de ítems de los tipos 31 y 44).

- [ ] **Step 7: Commit**

```bash
[ "$(git branch --show-current)" = "dev" ] && git add ZynstormECFPlatform.Data/Seeds/BusinessSimulationSeeds.cs ZynstormECFPlatform.Tests/Certification/RentaVehiculosSeedTests.cs && git commit -m "feat: tipo de negocio Renta de Vehículos en el seed de simulación

BusinessType 15 con 10 muestras temáticas de alquiler sobre las
plantillas validadas de Transporte. Gravadas con ITBIS 18% en los
tipos 31, 32, 33, 34, 41 y 45; 43, 44, 46 y 47 conservan su indicador.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>" && git branch --show-current
```

---

### Task 2: Caso "Renta de Vehículos" en SimulationSampleGenerator

**Files:**
- Create: `ZynstormECFPlatform.Tests/Certification/RentaVehiculosGeneratorTests.cs`
- Modify: `ZynstormECFPlatform.Services/Certification/SimulationSampleGenerator.cs` (después del caso `"Clínica/Servicio Ambulatorio"`, línea 30)

**Interfaces:**
- Consumes: `SimulationSampleGenerator.GenerateJson(string businessType, string ecfType, string rnc = "133009889", decimal? forceAmount = null)` y `EcfItemRequestDto` (`NombreItem`, `CantidadItem`, `PrecioUnitarioItem`, `MontoItem`, `IndicadorFacturacion`, `IndicadorBienoServicio`).
- Produces: `GenerateJson("Renta de Vehículos", ...)` devuelve ítems de alquiler gravados y de servicio. La regeneración de muestras del servicio de simulación usa `type.Name`, por lo que el nombre debe coincidir exactamente con el `Name` del `BusinessType` (Task 1).

- [ ] **Step 1: Escribir la prueba que falla**

Crear `ZynstormECFPlatform.Tests/Certification/RentaVehiculosGeneratorTests.cs`:

```csharp
using System.Text.Json;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Certification;

namespace ZynstormECFPlatform.Tests.Certification;

public class RentaVehiculosGeneratorTests
{
    private static EcfInvoiceRequestDto Generate(string ecfType = "31") =>
        JsonSerializer.Deserialize<EcfInvoiceRequestDto>(
            SimulationSampleGenerator.GenerateJson("Renta de Vehículos", ecfType))!;

    [Fact]
    public void GenerateJson_BuildsRentalServiceItems()
    {
        var items = Generate().ECF.DetallesItems.Item;

        Assert.Equal(5, items.Count);
        Assert.Contains(items, i => i.NombreItem.StartsWith("Alquiler"));
        Assert.All(items, i =>
        {
            Assert.Equal("1", i.IndicadorFacturacion);      // gravado ITBIS 18%
            Assert.Equal("2", i.IndicadorBienoServicio);    // servicio
        });
    }

    [Fact]
    public void GenerateJson_TotalsMatchTheRentalItems()
    {
        var totals = Generate().ECF.Encabezado.Totales;

        Assert.Equal(7400m, totals.MontoGravadoTotal);
        Assert.Equal(1332.00m, totals.TotalITBIS);
        Assert.Equal(8732.00m, totals.MontoTotal);
    }

    [Fact]
    public void GenerateJson_DoesNotFallBackToGenericMerchandise()
    {
        var items = Generate().ECF.DetallesItems.Item;
        Assert.DoesNotContain(items, i => i.NombreItem == "Venta de Mercancía General");
    }
}
```

- [ ] **Step 2: Ejecutar la prueba y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter RentaVehiculosGeneratorTests 2>&1 | grep -E " error |Passed!|Failed!|Failed "`
Expected: `Failed!`, por ejemplo `Assert.Equal() Failure: Expected 5, Actual 1` (hoy cae en el caso genérico `_ =>`). Si hay errores de compilación por `TotalITBIS` o `MontoGravadoTotal`, ajustar el nombre de la propiedad al de `EcfTotalesRequest` en `ZynstormECFPlatform.Dtos/EcfRequestDtos.cs` y repetir.

- [ ] **Step 3: Agregar el caso al generador**

En `SimulationSampleGenerator.cs`, insertar después del bloque `"Clínica/Servicio Ambulatorio" => new List<EcfItemRequestDto> { ... },` y antes de `"Repuesto" =>`:

```csharp
            "Renta de Vehículos" => new List<EcfItemRequestDto>
            {
                // IndicadorBienoServicio = "2": todos son servicios de alquiler, gravados con ITBIS 18%.
                new() { NombreItem = "Alquiler Kia Picanto 2025 (Día)", CantidadItem = 1, PrecioUnitarioItem = 2400, MontoItem = 2400, IndicadorFacturacion = "1", IndicadorBienoServicio = "2" },
                new() { NombreItem = "Alquiler Nissan Sentra SV (Día)", CantidadItem = 1, PrecioUnitarioItem = 2100, MontoItem = 2100, IndicadorFacturacion = "1", IndicadorBienoServicio = "2" },
                new() { NombreItem = "Alquiler Scooter CFMOTO 150cc (Día)", CantidadItem = 2, PrecioUnitarioItem = 900, MontoItem = 1800, IndicadorFacturacion = "1", IndicadorBienoServicio = "2" },
                new() { NombreItem = "Entrega y Recogida a Domicilio", CantidadItem = 1, PrecioUnitarioItem = 500, MontoItem = 500, IndicadorFacturacion = "1", IndicadorBienoServicio = "2" },
                new() { NombreItem = "Casco y Accesorios", CantidadItem = 2, PrecioUnitarioItem = 300, MontoItem = 600, IndicadorFacturacion = "1", IndicadorBienoServicio = "2" }
            },
```

- [ ] **Step 4: Ejecutar las pruebas y comprobar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "RentaVehiculosGeneratorTests|RentaVehiculosSeedTests" 2>&1 | grep -E " error |Passed!|Failed!|Failed "`
Expected: `Passed!` con `Failed: 0`.

- [ ] **Step 5: Commit**

```bash
[ "$(git branch --show-current)" = "dev" ] && git add ZynstormECFPlatform.Services/Certification/SimulationSampleGenerator.cs ZynstormECFPlatform.Tests/Certification/RentaVehiculosGeneratorTests.cs && git commit -m "feat: caso Renta de Vehículos en SimulationSampleGenerator

Ítems de alquiler como servicio y gravados con ITBIS 18% para la
ruta de regeneración de muestras de simulación.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>" && git branch --show-current
```

---

### Task 3: Migración EF y verificación final

**Files:**
- Create (generados por `dotnet ef`): `ZynstormECFPlatform.Data/Migrations/<timestamp>_AddRentaVehiculosBusinessType.cs` y `.Designer.cs`
- Modify (generado): `ZynstormECFPlatform.Data/Migrations/StorageContextModelSnapshot.cs`

**Interfaces:**
- Consumes: el seed de la Task 1 (`BusinessType` 15 y muestras 142 a 151) a través del modelo de diseño.
- Produces: una migración que inserta esos datos y su `Down` que los borra.

- [ ] **Step 1: Confirmar que no hay cambios de modelo pendientes aparte del seed**

Run: `git status --short` (solo debe verse limpio o con archivos ajenos a Migrations)
Run: `dotnet ef migrations has-pending-model-changes --project ZynstormECFPlatform.Data 2>&1 | tail -3`
Expected: la herramienta informa cambios de modelo (el seed nuevo) y termina con código distinto de 0, p. ej. `Changes have been made to the model since the last migration`. Si dice `No changes have been made`, la Task 1 no quedó aplicada: parar y revisar.

- [ ] **Step 2: Generar la migración**

Run: `dotnet ef migrations add AddRentaVehiculosBusinessType --project ZynstormECFPlatform.Data 2>&1 | tail -3`
Expected: `Done.` y tres archivos nuevos o modificados en `ZynstormECFPlatform.Data/Migrations/`.

- [ ] **Step 3: Verificar que la migración solo inserta datos**

Run:
```bash
f=$(ls ZynstormECFPlatform.Data/Migrations/*_AddRentaVehiculosBusinessType.cs)
echo "InsertData: $(grep -c 'migrationBuilder.InsertData' "$f")  (esperado 2)"
echo "DeleteData: $(grep -c 'migrationBuilder.DeleteData' "$f")  (esperado 11)"
echo "Esquema (debe ser 0): $(grep -c -E 'CreateTable|AlterColumn|AddColumn|DropColumn|CreateIndex|DropTable|UpdateData' "$f")"
grep -c "Renta de Veh" "$f"
grep -o -E '\{ 1(4[2-9]|5[01]), 15,' "$f" | wc -l
```
Expected: `InsertData: 2`, `DeleteData: 11` (1 tipo + 10 muestras), `Esquema: 0`, `Renta de Veh` al menos 1, y 10 filas de muestras con ids 142 a 151 y `BusinessTypeId` 15.

Si `Esquema` no es 0 o aparecen `UpdateData` de muestras existentes, parar: el modelo tiene otros cambios o el seed alteró ids existentes. No continuar; revisar la causa antes de commitear.

- [ ] **Step 4: Verificar el SQL sin tocar ninguna base de datos**

Run:
```bash
prev=$(ls ZynstormECFPlatform.Data/Migrations/*_UpdateClinicaSimulationServiceItems.cs | xargs -n1 basename | sed 's/\.cs$//')
dotnet ef migrations script "$prev" AddRentaVehiculosBusinessType --project ZynstormECFPlatform.Data --output "$TEMP/renta_vehiculos.sql" 2>&1 | tail -2
grep -c 'INSERT INTO "BusinessType"' "$TEMP/renta_vehiculos.sql"
grep -c 'INSERT INTO "BusinessSimulationSample"' "$TEMP/renta_vehiculos.sql"
```
Expected: `Done.`, luego `1` y `1` (cada `INSERT` agrupa sus filas). El script se genera sin conexión; no se aplica a ninguna base.

- [ ] **Step 5: Confirmar que el modelo quedó al día y correr toda la suite**

Run: `dotnet ef migrations has-pending-model-changes --project ZynstormECFPlatform.Data 2>&1 | tail -1`
Expected: `No changes have been made to the model since the last migration.`

Run: `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed "`
Expected: `Failed: 1` con únicamente `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada` (falla previa, no relacionada) y todo lo demás pasando.

- [ ] **Step 6: Commit**

```bash
[ "$(git branch --show-current)" = "dev" ] && git add ZynstormECFPlatform.Data/Migrations && git status --short && git commit -m "Migración AddRentaVehiculosBusinessType

Inserta el tipo de negocio Renta de Vehículos (Id 15) y sus 10
muestras de simulación (ids 142 a 151).

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>" && git branch --show-current && git log --oneline -4
```

- [ ] **Step 7: Comprobación manual posterior (la hace el usuario al desplegar, no el agente)**

Tras aplicar la migración en el entorno de pruebas: abrir la simulación de certificación, abrir el selector de tipo de negocio y comprobar que aparece "Renta de Vehículos". Al elegirlo y pulsar "Iniciar Simulación de Negocio" deben cargar sus 10 comprobantes de prueba. Después se replica a `Staging` por separado.
