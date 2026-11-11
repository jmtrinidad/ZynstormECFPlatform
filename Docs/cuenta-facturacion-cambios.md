# Cuenta de facturación con pool de comprobantes — resumen de cambios

## Qué se agregó

Un revendedor (usuario Admin) ahora puede tener **un plan de comprobantes propio, compartido por todos sus clientes**. Los comprobantes aceptados de todos sus clientes se suman en un total mensual; si ese total supera el límite del plan, se cobra el **excedente por tramos**, igual que en los demás planes. El revendedor crea sus propios clientes y puede crear usuarios para que cada cliente dé seguimiento a sus envíos.

## Cómo funciona

- **Cuenta de facturación:** la crea el SA, con un dueño (Admin), un plan de comprobantes activo, precio propio opcional, meses por pago, descuento por adelanto y días de gracia. Un Admin tiene una sola cuenta.
- **Clientes:** el SA asigna clientes a la cuenta, y los que crea el dueño nacen dentro de ella. Un cliente de una cuenta se cobra por la cuenta; su plan propio deja de cobrarse.
- **Consumo:** cada comprobante aceptado suma una vez en el cliente y en el total de la cuenta. No se cuenta dos veces aunque se reintente.
- **Cupo por cliente (informativo):** el revendedor puede repartir el total con un cupo por cliente. Pasarlo solo muestra una alerta; el cliente sigue emitiendo y el cobro se calcula sobre el total de la cuenta.
- **Cobro:** mensualidad más excedente del pool de cada mes cerrado. Los pagos se registran por cuenta (mensualidad y/o excedentes), con recibo, historial y reactivación automática.
- **Suspensión:** si la cuenta no paga dentro de su período de gracia, se suspende y **ninguno de sus clientes puede emitir** (respuesta 403). Al registrar el pago se reactiva. El dueño recibe los avisos de pago por correo.

## Qué ve cada rol

| Rol | Ve y hace |
|---|---|
| **SA** | Crea y edita cuentas, asigna clientes, registra pagos, ve el consumo de las cuentas junto al de los clientes. |
| **Admin dueño** | Página **Mi cuenta**: pool del mes, excedente proyectado, consumo y cupo por cliente, historial de pagos (solo lectura). Crea clientes y usuarios para ellos. |
| **Usuario de un cliente** | Solo sus clientes y sus envíos. No ve consumo, pagos ni la cuenta. |

## Seguridad y permisos

- Un Admin solo puede asignar a sus usuarios clientes que él mismo ve (403 si no).
- Un Admin solo edita o borra usuarios que él creó (antes podía con cualquiera, incluido SA).
- Un Admin solo ve, sube y borra certificados de sus clientes.
- Formatos de RI: los endpoints por cliente exigen pertenencia, y la biblioteca global (tipos de cliente y plantillas) queda **solo para SA**.

## Pantallas nuevas o cambiadas

- **Mi cuenta** (`/cuenta`, Admin) — nueva.
- **Cuentas** (`/configuraciones/cuentas`, SA) — nueva: alta, edición y asignación de clientes.
- **Pagos** — tarjeta de cuentas con registrar pago e historial.
- **Consumo** — las cuentas aparecen como una fila más.
- **Clientes** — el plan propio se deshabilita con un aviso cuando el cliente factura por una cuenta.

## Base de datos

Dos migraciones aditivas: `AddBillingAccounts` (tablas de cuenta y consumo del pool, y columnas `BillingAccountId` y `QuotaDocuments` en cliente) y `AddBillingAccountPayments` (recibos de cuenta; `ClientPayment.ClientId` pasa a opcional, con la regla de que un recibo es de un cliente **o** de una cuenta). No hay datos que migrar: los clientes existentes quedan como estaban. La API aplica las migraciones al arrancar.

## Antes de dar rol Admin a un revendedor real

1. Verificar contra PostgreSQL (base dev2): el conteo del pool, la migración de pagos y el flujo de pago, suspensión y reactivación. Hasta ahora está probado con pruebas unitarias y compilación, no con la base ni en el navegador.
2. `CertificationController` no tiene `[Authorize]` y `FeController` expone `received-files` por `clientId` sin autenticación. Siguen abiertos.
3. Las cuentas asignadas antes de los permisos deben reasignarse una vez para que el dueño reciba acceso a sus clientes.
4. Confirmar que ningún Admin de Zynstorm suba formatos de RI hoy, porque esa biblioteca ahora es solo SA.

## Documentos

Spec: `Docs/superpowers/specs/2026-11-11-cuenta-facturacion-pool-design.md`. Planes: `Docs/superpowers/plans/2026-11-11-cuenta-facturacion-fase{1,2,3,4}-*.md`.
