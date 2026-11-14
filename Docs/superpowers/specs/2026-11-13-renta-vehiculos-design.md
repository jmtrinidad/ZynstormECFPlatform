# Tipo de negocio "Renta de Vehículos" — diseño

Fecha: 2026-11-13

## Objetivo

Agregar un tipo de negocio de alquiler de vehículos (autos, motos/scooters y buggies,
como la flota de una empresa de renta turística) al selector de la simulación de
certificación e-CF, con muestras temáticas para cada tipo de comprobante.

Alcance: solo el tipo de negocio y sus muestras de simulación. No incluye catálogo
de flota, reservas ni selección de vehículos al facturar.

## Enfoque

Mismo mecanismo que Clínica/Servicio Ambulatorio (commits `c9e4cc8` y `defd539`):
las muestras se generan en `BusinessSimulationSeeds` a partir de las plantillas ya
validadas de Transporte, sustituyendo nombres de ítems e insertando ítems de servicio
adicionales. No se escribe JSON a mano por muestra, para no perder los montos, RNC e
indicadores ya validados por la DGII.

## Datos

**BusinessType**

- `BusinessTypeId = 15`
- `Name = "Renta de Vehículos"`
- `Description = "Alquiler de vehículos, motos y buggies por día (servicio gravado con ITBIS 18%)."`
- `GuidId = "1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a"`
- `RegisteredAt = 2026-11-13`

**BusinessSimulationSample**

Una muestra por tipo de e-CF (31, 32, 33, 34, 41, 43, 44, 45, 46, 47), generadas por el
bucle existente del seed:

- Ids correlativos desde `max(id) + 1`, que hoy da 142 a 151.
- `GuidId = 00000000-0000-0000-0015-0000000000{ecfType}`.
- `Name = "Renta de Vehículos - Tipo {ecfType}"`.

## Contenido de las muestras

Todos los ítems llevan `itemType` 2 (servicio). Las plantillas de Transporte de las que se
parte son exentas (`billingIndicator` 4; la 46 usa 3), así que el indicador se cambia donde
la regla fiscal lo permite:

- Gravados con ITBIS 18%: tipos 31, 32, 33, 34, 41 y 45, con `"billingIndicator":1` y
  **`"taxPercentage":18` explícito**, igual que las muestras de Farmacia que la DGII acepta.
- Conservan el indicador de su plantilla, sin `taxPercentage`: 43 (gastos menores), 44
  (regímenes especiales), 46 (exportación, 3) y 47 (pagos al exterior). Los ítems
  adicionales del 44 también son exentos.

**Por qué `taxPercentage` explícito (2026-11-13).** La primera versión ponía solo
`billingIndicator` 1 y la DGII la rechazó con "TotalITBIS no es válido" y "TotalITBIS1 no
coincide con MontoGravado1 x ITBIS Tasa 1". El simulador (`OldCertificationSimulationService`
y `OldEcfGeneratorService`) calcula el ITBIS de cada ítem con `TaxPercentage`, que vale 0 si
la muestra no lo trae: el XML salía con monto gravado y ITBIS 0. Las muestras de Farmacia
(ids 2 y siguientes de los negocios originales) llevan `TaxPercentage` explícito y no
fallan. Las muestras gravadas de Electrodomésticos y Mueblería (ids 21 a 24) tenían el mismo
defecto y se corrigieron igual.

**Guarda a futuro.** `SeedSamplesItbisTests` genera el XML real de todas las muestras
sembradas y exige que `TotalITBIS1` sea 18% de `MontoGravadoI1` y que `TotalITBIS` coincida.
Una muestra gravada nueva sin `taxPercentage` hace fallar esa prueba antes de llegar a la DGII.

Se conservan los montos, RNC y demás campos de la plantilla.

| e-CF | Ítem principal (reemplaza al de la plantilla) | Ítems adicionales |
|---|---|---|
| 31 | ALQUILER KIA PICANTO 2025 (DIA) | ALQUILER NISSAN SENTRA SV (DIA) 2,100; ALQUILER SCOOTER CFMOTO 150CC (DIA) 900; ENTREGA Y RECOGIDA A DOMICILIO 500 |
| 32 | ALQUILER BUGGY CF MOTO UFORCE 600 (DIA) | ALQUILER SCOOTER SUZUKI AN 125 (DIA) 600; CASCO Y ACCESORIOS 300 |
| 33 | AJUSTE POR DIAS DE ALQUILER | ninguno |
| 34 | CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE | ninguno |
| 41 | MANTENIMIENTO Y LAVADO DE FLOTA | ninguno |
| 43 | REEMBOLSO CAJA CHICA COMBUSTIBLE | ninguno |
| 44 | ALQUILER SCOOTER TAURO DIO 110 (DIA) | ALQUILER SYM FNX 200CC (DIA) 900; ENTREGA Y RECOGIDA A DOMICILIO 500 |
| 45 | ALQUILER SCOOTER SYM FNX 200CC (DIA) | ALQUILER SCOOTER SUZUKI AN 125 (DIA) 600; CASCO Y ACCESORIOS 300 |
| 46 | ALQUILER DE VEHICULO A TURISTA EXTRANJERO | ninguno |
| 47 | ALQUILER DE VEHICULO EN EL EXTERIOR | ninguno |

Precios de los ítems adicionales, en RD$ por día: los de la flota de referencia
convertidos a 60 RD$ por dólar. Scooter de 600 a 900, Nissan Sentra 2,100, Kia Picanto
2,400, buggy 3,900. Los ítems de entrega y accesorios llevan precio fijo razonable
(entrega 500, casco y accesorios 300) y cantidad 1.

Los tipos 33, 34, 41, 43, 46 y 47 mantienen un solo ítem para no alterar sus reglas de
referencia, igual que en Clínica. El monto de las muestras que lo requieren no se
modifica; por ejemplo, el tipo 32 conserva el monto de "gran monto" (>= 250,000)
exigido por su regla, aunque no sea realista para un alquiler diario.

## Cambios por archivo

- `ZynstormECFPlatform.Data/Seeds/BusinessSimulationSeeds.cs`: el `BusinessType` 15, el
  mapa de nombres de ítems, el mapa de ítems adicionales y su aplicación en el bucle
  (misma estructura que el bloque de Clínica).
- `ZynstormECFPlatform.Services/Certification/SimulationSampleGenerator.cs`: caso
  `"Renta de Vehículos"` con ítems de alquiler (servicio, gravados). Es la ruta que
  regenera las muestras desde el servicio de simulación; la migración usa el seed.
- Migración `AddRentaVehiculosBusinessType`, generada con `dotnet ef`, con el
  `InsertData` del tipo y de las 10 muestras, y su `Down`.
- `ZynstormECFPlatform.Tests`: prueba que verifica que el tipo 15 produce las 10 muestras
  y que cada `JsonData` es JSON válido con al menos un ítem de servicio.
- Frontend: sin cambios. Se comprobó que no hay listas fijas de tipos de negocio en el
  frontend ni en el backend fuera del seed y del generador; el selector los carga de la API.

## Fuera de alcance

- Catálogo de flota, fotos, precios por día/semana/mes y reservas.
- Réplica a la rama `Staging` y despliegue. Se hacen después, aparte.

## Verificación

- Las pruebas nuevas y la suite existente pasan (la falla previa de
  `AprobacionComercialResultTests` no está relacionada).
- La migración se genera sin cambios inesperados en el snapshot.
- Tras aplicar la migración, el selector de la simulación lista "Renta de Vehículos" y
  carga sus 10 muestras.
