# Guía: cuenta de facturación con pool de comprobantes

Cómo configurar y usar un plan de comprobantes compartido por todos los clientes de un revendedor. Para qué sirve y qué cambió está en `cuenta-facturacion-cambios.md`.

## Conceptos en 30 segundos

- **Cuenta:** el plan del revendedor. Tiene un dueño (usuario Admin), un plan de comprobantes y sus fechas de pago. Un Admin tiene una sola cuenta.
- **Pool:** la suma de los comprobantes aceptados del mes de todos los clientes de la cuenta. Se compara contra el límite del plan.
- **Excedente:** lo que pasa del límite, cobrado por los tramos del plan (por ejemplo, 1–100 a RD$6 y 101 en adelante a RD$9).
- **Cupo:** una guía opcional por cliente para repartir el pool. Pasarlo solo muestra una alerta; nunca bloquea.
- **Cliente suelto:** un cliente sin cuenta. Sigue funcionando exactamente igual que antes.

## Antes de empezar (una sola vez)

1. **Migraciones aplicadas** en la base: `AddBillingAccounts` y `AddBillingAccountPayments`. La API las aplica sola al arrancar.
2. Un **plan de comprobantes activo** con sus tramos de excedente, en Configuraciones → Planes. Los planes de renta no sirven para cuentas.
3. Un **usuario con rol Admin** para el revendedor, creado por el SA en Configuraciones → Usuarios.
4. Que ese Admin **no tenga ya una cuenta** (el sistema no lo ofrece como dueño si la tiene).

> Ojo: el `DefaultConnection` activo de `appsettings.Development.json` apunta a producción. Para probar usa la base dev2 con una cadena por variable de entorno (`ConnectionStrings__DefaultConnection`) y nunca ejecutes `dotnet ef database update` con la configuración por defecto.

## Para el SA

### 1. Crear la cuenta

**Configuraciones → Cuentas → Nueva cuenta** (`/configuraciones/cuentas`).

| Campo | Qué poner |
|---|---|
| Nombre | El nombre de la cuenta (máx. 100 caracteres). |
| Usuario dueño | Un Admin sin cuenta. |
| Plan de comprobantes | Solo aparecen planes de comprobantes activos. |
| Precio especial | Opcional. Reemplaza el precio del plan y su oferta. |
| Meses por pago | 1 a 60. Con más de uno, se cobra por adelantado. |
| Descuento por adelanto | 0 a 100 %. |
| Días de gracia | 1 a 30. Pasado el vencimiento más estos días, la cuenta se suspende. |
| Último / próximo pago | Si ya hay un ciclo en curso. El próximo pago es la fecha desde la que se vence. |

Por API: `POST /v1/BillingAccount` (rol SA).

### 2. Asignarle clientes

En la fila de la cuenta, **Clientes**. Marca los clientes que facturan por la cuenta y guarda.

- Los clientes que ya están en otra cuenta salen deshabilitados; suéltalos primero de esa.
- El dueño recibe acceso a los clientes asignados, así los ve en Clientes, Facturas y el Dashboard.
- El plan propio del cliente se conserva pero **deja de cobrarse**.
- Conviene asignar a **inicio de mes**: si el cliente ya tenía consumo ese mes con su plan anterior, esa fila conserva su cobro individual.
- Soltar un cliente de la cuenta no le quita el acceso al dueño (puede haberlo creado él). Si hace falta quitarlo, edita al usuario.

Por API: `PUT /v1/BillingAccount/guid/{guid}/clients` con `{ "clientGuidIds": [...] }`. La lista reemplaza a los miembros actuales.

### 3. Seguir el consumo

**Consumo** muestra las cuentas junto a los clientes, con una insignia "Cuenta": comprobantes del pool, límite, excedente, mensualidad y total del mes. Despliega la fila para ver el detalle por tramos.

### 4. Registrar un pago

**Pagos → Cuentas de facturación → Registrar pago**.

1. Marca la **mensualidad** (con los meses y el descuento de la cuenta) y/o los **excedentes pendientes** de meses cerrados.
2. Elige fecha, método y referencia, y registra.
3. Se genera el recibo. La mensualidad avanza el próximo pago desde la fecha que vencía (no desde el día en que se pagó).
4. Si la cuenta estaba suspendida y el nuevo próximo pago queda en el futuro, **se reactiva sola** y sus clientes vuelven a emitir.

Un excedente solo se puede pagar una vez. Solo se cobran meses cerrados: el excedente del mes en curso aparece como proyección, no como deuda.

Por API: `GET .../payments/preview`, `POST .../payments` y `GET .../payments` bajo `/v1/BillingAccount/guid/{guid}`.

### 5. Editar o reactivar

**Cuentas → editar** cambia plan, precio, meses, descuento, días de gracia y fechas. Poner un próximo pago futuro a una cuenta suspendida la reactiva. El dueño no se puede cambiar.

## Para el Admin dueño

### Mi cuenta

**Mi cuenta** (`/cuenta`) muestra:

- El pool del mes contra el límite del plan, con la barra en rojo si ya hay excedente.
- El **excedente proyectado** y la mensualidad.
- El próximo pago y su estado (al día, por vencer, vencido).
- El consumo de cada cliente este mes.
- El **historial de pagos** (solo lectura: los pagos los registra el SA).

Si la cuenta está suspendida verás un aviso rojo: ninguno de tus clientes puede emitir hasta que se registre el pago.

### Fijar el cupo de un cliente

En la tabla de consumo, escribe el cupo y pulsa **Guardar**. Déjalo vacío para quitarlo. Cuando un cliente supera su cupo aparece "Pasó su cupo", pero sigue emitiendo.

### Crear clientes

En **Clientes → Nuevo cliente**. El cliente nace dentro de tu cuenta y se cobra por ella; el selector de plan sale deshabilitado a propósito.

### Darle acceso a un cliente (usuarios del cliente)

1. **Configuraciones → Usuarios → Nuevo usuario.** Los usuarios que creas tienen rol Normal.
2. Asígnale el o los clientes. Solo puedes asignar clientes que tú ves; si intentas otro, el sistema responde 403.
3. Si quieres limitar sus pantallas, ajusta sus permisos por página.

Ese usuario ve solo sus clientes y sus envíos. No ve consumo, pagos ni la cuenta. Tú puedes editar o borrar únicamente a los usuarios que creaste.

## Qué pasa solo (jobs)

| Hora (RD) | Qué hace |
|---|---|
| 12:05 AM | Suspende las cuentas y los clientes sueltos cuyo vencimiento más los días de gracia ya pasó. |
| 8:00 AM | Envía el primer aviso (un día después del vencimiento) y el último (al llegar los días de gracia) al correo del dueño, y un resumen al correo administrativo. |

Con vencimiento el día D y g días de gracia: D+1 primer aviso, D+g último aviso, D+g+1 suspensión. Cada aviso se envía una sola vez por fecha de pago.

## Cómo verificarlo de punta a punta (en dev2)

1. SA: crea una cuenta con un Admin y un plan de comprobantes, y asígnale dos clientes. Debe aparecer en Cuentas con 2 clientes.
2. Emite un comprobante de cada cliente. En Cuentas, el pool del mes debe subir en 2 y en **Mi cuenta** cada cliente debe mostrar el suyo.
3. Fija un cupo de 0 a un cliente: debe mostrar "Pasó su cupo" y seguir emitiendo.
4. SA: en Pagos, registra la mensualidad. El próximo pago avanza y hay un recibo con "Cuenta".
5. Pon el próximo pago de la cuenta en una fecha pasada (más allá de la gracia) y corre el job de suspensión. La cuenta queda "Suspendida" y la emisión de sus clientes responde 403.
6. Registra un pago: la cuenta se reactiva y la emisión vuelve.
7. Con un usuario Normal: no debe ver Mi cuenta, Cuentas, Consumo ni Pagos.

## Problemas comunes

| Síntoma | Causa y solución |
|---|---|
| No puedo elegir un usuario como dueño | No es Admin o ya tiene una cuenta. |
| "El cliente X ya pertenece a otra cuenta" | Suéltalo primero de la otra cuenta. |
| Un Admin recibe 403 al crear un usuario | Asignó un cliente que no ve. Debe ser de su cuenta o creado por él. |
| El dueño no ve un cliente que el SA le asignó antes de los permisos | Repite la asignación de clientes de esa cuenta para que se cree su acceso. |
| El cliente de una cuenta aparece con $0 en Consumo | Es normal: el cobro sale de la fila de la cuenta, no de la del cliente. |
| Un cliente suelto ya no aparece en Pagos | Pasó a una cuenta: su pago se registra en la cuenta. |
| Un Admin de Zynstorm ya no puede subir formatos de RI | La biblioteca de formatos ahora es solo SA. |
| La cuenta no se reactiva tras un pago | El pago no incluyó la mensualidad, o el nuevo próximo pago sigue vencido. |

## Pendiente antes de dar rol Admin a un revendedor real

- Verificar contra PostgreSQL (dev2): hasta ahora está probado con pruebas unitarias y compilación, no con la base ni en el navegador.
- `CertificationController` no tiene `[Authorize]` y `FeController` expone `received-files` por `clientId` sin autenticación.
