# Plan de renta con tope de usuarios — Diseño

Fecha: 2026-09-15

Extiende el diseño de [planes de cliente y consumo mensual](2026-09-14-client-plans-monthly-usage-design.md).

## Objetivo

Agregar un segundo tipo de plan, **Renta**, para clientes que pagan una renta fija y tienen un tope
fijo de usuarios. Sobre el cliente se registran último pago, próximo pago, si pagó el año completo y
un descuento por pago adelantado.

## Decisiones

- Los dos tipos de plan son **excluyentes**: el cliente sigue teniendo un solo `PlanId`.
- El tope de usuarios es **informativo**: no bloquea la creación ni la asignación de usuarios.
  El control de concurrencia del API se maneja en otro proyecto, fuera de esta plataforma.
- El tope vive en el **plan** (`Renta 5 usuarios` y `Renta 10 usuarios` son dos planes distintos).
- Los datos de pago son **campos del cliente**, sin historial de cobros.
- El descuento es un **porcentaje** (0–100), por defecto 0.
- Los montos de renta se **calculan al vuelo** desde cliente + plan; no hay snapshot mensual.

## Reglas de negocio

- Plan de renta: `MonthlyFee` **es** la renta mensual. Al guardar, el backend **fuerza**
  `MonthlyDocumentLimit = -1` (no lo valida, lo sobreescribe) y **rechaza** el plan si trae tramos de
  excedente.
- El descuento aplica al ciclo configurado, sea mensual o anual; **no** está condicionado al check de
  año completo. Un cliente que paga mes a mes puede tener descuento si así se acordó.
- Cliente con plan de renta: **no** se registra consumo mensual; `RegisterAcceptedAsync` sale temprano.
  Emite comprobantes sin límite y sin excedente.
- Cliente sin plan o con plan inactivo: comportamiento actual, sin cambios.
- `MaxUsers` solo se muestra. Se expone `ActiveUsersCount` (filas de `UserClients` cuyo usuario está
  activo y no eliminado) para presentar "3 de 5 usuarios".
- **Renta vencida no desactiva al cliente.** Solo genera aviso. Desactivar sigue siendo la acción
  manual sobre `ClientInactive`, que ya bloquea `emit` con `403`.
- `NextRentPaymentDate` la **sugiere el frontend** como `LastRentPaymentDate + (RentPaidFullYear ? 12 : 1)`
  meses al cambiar la fecha de último pago o el check de año completo, y queda editable para acuerdos
  fuera de norma. El backend solo valida que no sea anterior a `LastRentPaymentDate`.
- Las fechas son fechas calendario (hora 00:00) y los días se cuentan en hora de República Dominicana
  (`ToDrTime()`), igual que el aviso de vencimiento de certificados.

### Fórmula de la renta

```
meses  = RentPaidFullYear ? 12 : 1
bruto  = MonthlyFee × meses
total  = bruto × (1 - RentDiscountPercent / 100)
```

Ejemplo: renta RD$2,000, año completo, 10% de descuento → 12 × 2,000 × 0.9 = RD$21,600.

### Monto del mes consultado (reporte de `/consumo`)

```
due = NextRentPaymentDate
due == null                      → monto = ciclo completo, estado SinFecha
due posterior al mes consultado  → monto = 0              (cubierto)
due en el mes consultado o antes → monto = ciclo completo (toca renovar, o vencido)
```

Un cliente que pagó el año completo aparece en 0 durante los meses cubiertos y con el ciclo completo
en el mes de renovación. Si la fecha ya pasó, sigue apareciendo con el monto pendiente hasta que se
registre el pago y la fecha avance.

### Semáforo de renta

| Estado | Condición |
|---|---|
| `SinFecha` | `NextRentPaymentDate = null` |
| `Vencido` | `NextRentPaymentDate` < hoy (hora RD) |
| `PorVencer` | faltan `RentPaymentWarningDays` días o menos |
| `AlDia` | falta más que eso |

`RentPaymentWarningDays` va en `AppSettings` con default 15, mismo patrón que
`CertificateExpirationWarningDays`.

## Modelo de datos (una migración)

| Entidad | Cambio |
|---|---|
| `Plan` | `+ PlanTypeId` int default 1, `+ MaxUsers` int? |
| `Client` | `+ LastRentPaymentDate` DateTime?, `+ NextRentPaymentDate` DateTime?, `+ RentPaidFullYear` bool default false, `+ RentDiscountPercent` decimal(5,2) default 0 |

Nuevo enum `PlanTypeEnum { Documents = 1, Rent = 2 }` en `ZynstormECFPlatform.Core/Enums/`, al estilo de
`StatusEnum`. Los planes existentes quedan en `Documents` por el default de la columna.

`MaxUsers` es null en planes de comprobantes; en planes de renta es `> 0`, o `-1` para ilimitado
(misma convención que `MonthlyDocumentLimit`).

## Componentes backend

1. **`RentCalculator`** en `ZynstormECFPlatform.Services/Billing/`, hermana de `BillingCalculator`,
   función pura sin dependencias:
   - `Calculate(monthlyFee, paidFullYear, discountPercent)` → `RentCalculationResult(MonthlyFee,
     MonthsCovered, GrossAmount, DiscountPercent, DiscountAmount, Total)`.
   - `GetAmountForMonth(result, nextPaymentDate, year, month)` → monto del mes según la tabla anterior.
   - `GetRentStatus(nextPaymentDate, warningDays)` → `(RentStatus, DaysToDue)`.
2. **Validación de plan**: `BillingCalculator.ValidatePlan` recibe el tipo de plan y acumula los errores
   nuevos — Renta exige `MaxUsers` (`> 0` o `-1`) y lista de tramos vacía; Documents exige `MaxUsers = null`.
3. **`ClientUsageService.RegisterAcceptedAsync`**: retorna sin hacer nada si `plan.PlanTypeId` es `Rent`.
4. **DTOs**
   - `PlanCreateDto` / `PlanViewDto`: `+ PlanTypeId`, `+ MaxUsers`.
   - `ClientCreateDto`: `+ LastRentPaymentDate`, `+ NextRentPaymentDate`, `+ RentPaidFullYear`,
     `+ RentDiscountPercent` (`[Range(0, 100)]`).
   - `ClientViewDto`: los anteriores `+ PlanTypeId`, `+ MaxUsers`, `+ ActiveUsersCount`, `+ RentStatus`,
     `+ RentDaysToDue`, `+ RentCycleAmount`.
   - `ClientMonthlyUsageDto`: `+ PlanTypeId`. En una fila de renta los campos de documentos van en
     cero (`AcceptedDocuments = 0`, `OverageDocuments = 0`, `OverageAmount = 0`, `Tiers` vacío),
     `MonthlyDocumentLimit = -1`, `MonthlyFee` es la renta mensual y `Total` es el monto del mes
     (0 si está cubierto). El frontend usa `PlanTypeId` para decidir qué columnas mostrar.
   - Nuevo `ClientRentDto`: cliente (guid, nombre, RNC, inactivo), plan (nombre, renta mensual,
     usuarios permitidos), `ActiveUsersCount`, `RentPaidFullYear`, `RentDiscountPercent`,
     `MonthsCovered`, `GrossAmount`, `DiscountAmount`, `Total`, `LastRentPaymentDate`,
     `NextRentPaymentDate`, `RentStatus`, `RentDaysToDue`. `Total` aquí es el mismo valor que
     `ClientViewDto.RentCycleAmount`: el ciclo completo con descuento aplicado.
5. **Endpoints** (`ClientController`, patrón `BaseController`, solo rol SA)
   - `GET v1/Client/rent` — clientes cuyo plan es de tipo Renta y está activo, con estado y montos ya
     calculados. Incluye los clientes con `ClientInactive = true`, marcados con la bandera, para que no
     desaparezcan del cobro sin verse.
   - `GET v1/Client/usage?year=&month=` — hoy solo lee filas de `ClientMonthlyUsage`. Pasa a **unir**
     esas filas con los clientes de plan de renta, cuyo monto del mes se calcula al vuelo.
   - `FillRentStatusAsync(IEnumerable<ClientViewDto>, ct)` — hermana de `FillCertificateExpirationAsync`,
     llena estado de renta, días y `ActiveUsersCount` en los listados de clientes.

## Frontend (Next.js)

- `/configuraciones/planes`: selector de tipo de plan. Al elegir Renta se ocultan el límite mensual de
  documentos y el editor de tramos, y aparece "Usuarios permitidos" con check "Ilimitado" → `-1`,
  reusando el patrón del límite mensual.
- `/clientes`: los cuatro campos de renta en el formulario, visibles solo cuando el plan seleccionado es
  de renta; el próximo pago se auto-sugiere al cambiar el último pago o el check de año completo y queda
  editable. En la tabla: badge de renta vencida / por vencer y contador "3 de 5 usuarios".
- `/renta`: página nueva con tabla de clientes de renta, semáforo, descuento y total del ciclo.
  Nuevos `services/rent.service.ts` y `types/rent.type.ts`.
- `/consumo`: la tabla acepta filas de renta — columnas de documentos vacías, monto del mes y la marca
  "Cubierto" cuando el monto es 0.
- Dashboard: tarjeta de rentas vencidas / por vencer, al lado del aviso de certificados.
- `types/plan.type.ts`: `planTypeId`, `maxUsers` y la constante de ilimitado para usuarios.

## Pruebas

- `RentCalculator.Calculate`: mensual sin descuento; año completo (×12); año completo con 10%
  (12 × renta × 0.9 = RD$21,600 con renta 2,000); descuento 0 por defecto.
- `RentCalculator.GetAmountForMonth`: mes cubierto → 0; mes de renovación → ciclo completo; fecha
  vencida → ciclo completo; `NextRentPaymentDate = null` → ciclo completo.
- `GetRentStatus` en los bordes: ayer → `Vencido`; hoy → `PorVencer`; hoy+15 → `PorVencer`;
  hoy+16 → `AlDia`; null → `SinFecha`.
- Validación de plan: Renta sin `MaxUsers` → error; Renta con tramos → error; Comprobantes con
  `MaxUsers` → error; Renta con `MaxUsers = -1` → válido.
- `ClientUsageService`: cliente con plan de renta no crea ni incrementa fila de consumo.
- `GET v1/Client/usage`: un mes con clientes de ambos tipos devuelve las filas de consumo y las de renta.

## Fuera de alcance

- Historial de pagos de renta (tabla `ClientRentPayment`). Si más adelante hace falta, se agrega encima
  de este diseño sin tocar los campos del cliente.
- Corte automático del servicio por renta vencida.
- Bloqueo real al superar el tope de usuarios.
- Control de concurrencia de llamadas al API (se maneja en otro proyecto).
- Generación automática de la factura del cliente.
