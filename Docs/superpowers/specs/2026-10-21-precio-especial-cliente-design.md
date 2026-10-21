# Precio especial por cliente — diseño

Fecha: 2026-10-21

## Objetivo

Permitir fijar a un cliente un precio mensual propio, distinto del precio de su plan, para los acuerdos puntuales que se necesitan de vez en cuando. Complementa las ofertas de plan (spec `2026-10-21-ofertas-en-planes-design.md`).

## Decisiones

| Tema | Decisión |
|---|---|
| Precio propio y oferta del plan | El precio propio manda siempre: reemplaza el precio del plan **y** su oferta para ese cliente |
| Cambio de plan del cliente | El precio propio se conserva; solo se quita a mano |
| Alcance | Solo la mensualidad o renta. Límite de comprobantes y tramos de excedente siguen siendo los del plan |
| Quién lo edita | Solo el rol SA |
| Consumo (`/consumo`) | Muestra el precio propio cuando exista (cambia lo que se dejó intacto en el spec de ofertas) |

## 1. Datos

- `Client.CustomMonthlyFee` (`decimal?`). Null = usa el precio del plan. Se acepta `0` o más; no se compara con el precio del plan (el precio pactado puede ser mayor o menor).
- Migración con una columna nullable (`numeric(18,2)`, la convención de decimales ya aplica). Los clientes existentes quedan sin precio propio.

## 2. Reglas de cobro

### Precio vigente
- `PaymentCalculator.GetMonthlyFee(Client client)` devuelve `client.CustomMonthlyFee ?? client.Plan.MonthlyFee`. Exige `Plan` cargado, igual que `CalculateCycle`.
- `PaymentCalculator.CalculateCycle(Client, DateTime)` usa ese precio. Si el cliente tiene precio propio, **no** aplica la oferta del plan (`offer = null`). El descuento por pago adelantado (`PrepaymentDiscountPercent`) se aplica sobre el bruto resultante.
- El resto de los consumidores deja de leer `Plan.MonthlyFee` directo y pasa por `GetMonthlyFee` o por `CalculateCycle`:
  - `ClientPaymentRegistrationService`: la validación "el plan del cliente no tiene mensualidad a pagar" y la condición que decide si se muestra la mensualidad en la previsualización (hoy `Plan.MonthlyFee > 0` / `<= 0`).
  - `ClientController`: `MonthlyFee` de las filas de renta mensual y de `GetPaymentClients`, y `PlanMonthlyFee` usado para `PaymentCycleAmount`.
  - `PaymentReminderService`: ya usa `CalculateCycle`, no requiere cambio.

### Oferta
- `OfferStartDate` no se toca. Mientras el cliente tenga precio propio, `ClientController.FillOffer` lo trata como sin oferta (`OfferStatus = 0`, sin fin ni días restantes).
- Si se quita el precio propio y la oferta del plan aún no terminó, vuelve a aplicar. El reloj siguió corriendo durante todo el periodo con precio propio, de modo que puede haber terminado.

### Cambio de plan
- Cambiar el plan del cliente no modifica `CustomMonthlyFee`.

### Consumo
- `BuildUsageDtosAsync` usa `CustomMonthlyFee` en lugar de `ClientMonthlyUsage.MonthlyFee` (la copia del precio del plan al crear el mes) cuando el cliente tiene precio propio. El excedente no cambia. Para esto se incluye el cliente completo, que ya viene con `Include(u => u.Client)`.
- La copia `ClientMonthlyUsage.MonthlyFee` sigue guardando el precio del plan.

## 3. Permisos

- Solo el rol SA puede establecer o quitar el precio propio.
- En `ClientController.Put`, si quien guarda no es SA, se ignora `CustomMonthlyFee` del cuerpo y se conserva el valor actual del cliente.
- En `Post`, si quien crea no es SA, `CustomMonthlyFee` se fuerza a null.
- Ambas reglas salen de una función pura, `ClientPricing.ResolveCustomFee(bool isSA, decimal? requested, decimal? current)` en `Services/Billing`: devuelve `requested` si es SA y `current` si no. En `Post` se llama con `current = null`.

## 4. API

- `ClientCreateDto.CustomMonthlyFee (decimal?)`, con `[Range(0, 99999999)]`.
- `ClientViewDto` agrega `EffectiveMonthlyFee (decimal?)` (precio vigente; null si el cliente no tiene plan) y `HasCustomPrice (bool)`. `PlanMonthlyFee` sigue siendo el precio del plan. `CustomMonthlyFee` se mapea por nombre.
- `EffectiveMonthlyFee` y `HasCustomPrice` los calcula `ClientController` junto con los campos de oferta (`FillOffer`), no AutoMapper.
- `PaymentCycleAmount` se calcula con el precio vigente.

## 5. Frontend

- **Diálogo del cliente** (`app/clientes/page.tsx`): campo "Precio especial (RD$)" debajo del plan, vacío por defecto, con la nota "Reemplaza el precio del plan y su oferta". Solo visible y editable para SA (`user?.userType === 1`, como en la página de planes). El estimado del total del pago usa ese precio y no aplica oferta cuando está definido.
- **Lista `/clientes`:** la columna Precio muestra el precio vigente; si es precio propio, agrega la etiqueta "Precio especial" y el precio del plan en el tooltip. El badge de oferta no aparece (el servidor ya devuelve `OfferStatus = 0`).
- **Panel de oferta del diálogo:** si el cliente tiene precio propio, muestra "El precio especial de este cliente reemplaza la oferta del plan" en lugar de las fechas.
- **Pago y recibo:** muestran el precio vigente sin cambios de diseño.
- `Client`/`ClientCreate`/`ClientUpdate` (types) agregan `customMonthlyFee`, `effectiveMonthlyFee`, `hasCustomPrice`.

## 6. Pruebas

En `ZynstormECFPlatform.Tests/Billing`:
- `GetMonthlyFee`: con y sin precio propio; `0` es un precio propio válido (no cae al del plan); sin plan lanza `ArgumentException`.
- `CalculateCycle`: el precio propio reemplaza al del plan; no aplica la oferta aunque `OfferStartDate` esté vigente; descuento de prepago sobre el precio propio; precio propio mayor y menor que el del plan; sin precio propio el resultado es idéntico al actual (regresión).
- Regla de permisos: función pura que decide el valor final de `CustomMonthlyFee` según `isSA` (SA usa el valor recibido; no SA conserva el actual).
