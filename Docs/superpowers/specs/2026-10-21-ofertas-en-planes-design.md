# Ofertas en planes — diseño

Fecha: 2026-10-21

## Objetivo

Permitir que un plan tenga una oferta: un precio promocional durante X meses, y al terminar ese tiempo se cobra el precio real del plan. Los administradores deben poder ver, por cliente, cuánto tiempo le falta a la oferta.

## Decisiones

| Tema | Decisión |
|---|---|
| Cómo se define el descuento | Precio promocional fijo (`OfferPrice`), no porcentaje |
| Cuándo empieza la oferta de un cliente | Al asignarle el plan (no al primer pago) |
| Pago por adelantado que cruza el fin de la oferta | Prorrateado por mes: cada mes se cobra a su precio |
| Seguimiento | Columna/badge y filtro en `/clientes` + sección en el detalle del cliente |
| Fuera de alcance | Aviso por correo y página de reporte de ofertas |

## 1. Datos

### `Plan`
- `OfferPrice` (`decimal?`): precio mensual durante la oferta.
- `OfferMonths` (`int?`): duración de la oferta en meses.
- `MonthlyFee` sigue siendo el precio real.
- Un plan tiene oferta si ambos campos tienen valor.
- Validación (en `PlanCreateDto`/`PlanController`):
  - Los dos campos van juntos o ninguno.
  - `OfferMonths` ≥ 1.
  - `0` ≤ `OfferPrice` < `MonthlyFee` (si no es menor, no es una oferta).

### `Client`
- `OfferStartDate` (`DateTime?`): fecha calendario (hora RD) en que arrancó la oferta del cliente.
- Se fija al asignar o cambiar el plan:
  - Plan nuevo con oferta → `OfferStartDate = hoy`.
  - Plan nuevo sin oferta → `null`.
  - Cambio a otro plan con oferta → la oferta se reinicia (`hoy`).
  - Edición del cliente sin cambiar de plan → no se toca.
- Fin de oferta = `OfferStartDate + OfferMonths` meses. No se persiste, se calcula.

### Migración EF
- Agrega `Plan.OfferPrice`, `Plan.OfferMonths`, `Client.OfferStartDate`, todos nulos.
- Planes y clientes existentes quedan sin oferta. Un plan al que se le agrega oferta después no afecta retroactivamente a los clientes que ya lo tenían; la oferta corre solo desde la asignación.

## 2. Lógica de cobro

### `OfferPricing` (función pura, en `Services/Billing`)
Dado `OfferPrice`, `OfferMonths`, `OfferStartDate` y una fecha de referencia (hoy RD, parametrizable para pruebas), devuelve:
- Estado: `None`, `Active` o `Ended`.
- Fecha de fin de oferta.
- Días restantes y meses restantes.
- Precio que corresponde a una fecha dada (oferta si cae antes del fin; real si cae en el fin o después).

### `PaymentCalculator.Calculate`
- Pasa a cobrar mes por mes. Cada mes cubierto por el pago usa el precio de oferta si ese mes cae antes del fin de oferta, y el precio real si cae después.
- Regla por mes: el mes k (0-based) del pago cubre el periodo que empieza en `inicioPeriodo + k meses`, donde `inicioPeriodo` es el mismo que usa `PaymentRegistrationPolicy.CalculateNewNextPaymentDate` para encadenar la fecha de próximo pago. Ese mes usa `OfferPrice` si su fecha de inicio es anterior al fin de oferta, y `MonthlyFee` en caso contrario. La oferta se mide desde `OfferStartDate`, no desde la fecha de pago.
- Ejemplo: oferta de 3 meses, el cliente paga 6 → 3 meses a `OfferPrice` + 3 a `MonthlyFee`.
- `GrossAmount` es la suma de los meses a su precio. `PrepaymentDiscountPercent` se aplica sobre ese bruto.
- `PaymentCalculationResult` agrega el desglose (meses a precio de oferta, meses a precio real) para el recibo.
- Sin oferta, el resultado es idéntico al actual.

### Consumidores a actualizar
- `ClientPaymentRegistrationService` (previsualización y registro de pago; el recibo muestra el desglose).
- `PaymentReminderService` (monto del aviso de pago).
- `ClientController`, los dos puntos que calculan la mensualidad (~líneas 125 y 727).

### Sin cambios
`ClientMonthlyUsage.MonthlyFee` (snapshot mensual del plan para consumo y excedentes) sigue guardando el precio real. La oferta solo afecta el cobro de la mensualidad.

## 3. UI y API

### API
- `PlanCreateDto`/`PlanViewDto`: `OfferPrice`, `OfferMonths`.
- `ClientDtos`: `OfferStartDate`, y calculados en el servidor `OfferEndDate`, `OfferDaysRemaining`, `OfferMonthsRemaining`, `OfferStatus` (`None`/`Active`/`Ended`).
- AutoMapper (`MappingProfiles`) y tipos TS (`plan.type.ts`, `client.type.ts`) actualizados.

### Frontend
- `/configuraciones/planes`: campos "Precio de oferta" y "Meses de oferta" en el formulario; etiqueta de oferta en la lista.
- `/clientes`:
  - Columna/badge: "Faltan 45 días (hasta 15-ene-2027)", "Oferta finalizada", o vacío si no hay oferta.
  - Color de aviso cuando faltan 30 días o menos.
  - Filtro "Ofertas por vencer".
- Detalle del cliente: sección "Oferta" con inicio, fin, tiempo restante, precio de oferta vs. precio real y de cuánto sube la cuota al terminar.
- Recibo/previsualización de pago: muestra el desglose oferta/real cuando aplica.

## 4. Pruebas

En `ZynstormECFPlatform.Tests/Billing`, junto a `BillingCalculatorTests`:
- `OfferPricing`: oferta activa, vencida, sin oferta, borde del último día, fin de oferta a fin de mes.
- `PaymentCalculator`: pago dentro de la oferta, pago que cruza el fin, un solo mes, descuento de prepago combinado, sin oferta (regresión).
- Reglas de asignación de plan: cuándo se fija, se limpia o se reinicia `OfferStartDate`.
- Validación de plan: combinaciones inválidas de `OfferPrice`/`OfferMonths`.

## 5. Ajustes tomados al planificar

- **Desglose en el recibo.** `ClientPaymentItem` agrega `OfferMonthsCovered` (meses cobrados a precio de oferta) y `OfferUnitPrice`, para que el historial de recibos pueda mostrar el desglose oferta/real. Son 2 columnas nuevas además de las 3 del apartado 1.
- **`PaymentCalculator.CalculateCycle(Client, fallbackPeriodStart)`.** Un solo punto que arma el ciclo de un cliente con su plan y su oferta. El primer mes cubierto empieza en `NextPaymentDate`, o en `fallbackPeriodStart` si no hay fecha. Lo usan el registro y la previsualización de pago, el aviso de pago y los dos puntos de `ClientController` (renta mensual y `payments`).
- **Campos calculados.** Los calcula `ClientController` (no AutoMapper, porque `ZynstormECFPlatform.Mappings` no referencia `Services`). `ClientViewDto` también lleva `PlanOfferPrice` y `PlanOfferMonths`.
- **Filtro "Ofertas por vencer".** El listado de clientes está paginado en el servidor, así que el filtro es el parámetro `offerExpiring=true` de `GET /Client`: se buscan en memoria los clientes con oferta activa que vencen en 30 días o menos y se restringe la consulta a esos ids.
- **Fuera de alcance, sin cambio.** `BuildUsageDtosAsync` (consumo de planes por comprobantes) sigue calculando la mensualidad con el precio real del snapshot `ClientMonthlyUsage.MonthlyFee`.
