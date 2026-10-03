# Reenvío / generación de API Key de clientes — Diseño

Fecha: 2026-10-03

## Problema

1. No existe forma de volver a enviar al cliente sus credenciales (API Key + Secret Key) por correo. Solo se envían una vez, al crear el cliente.
2. En el modal "API Key de {cliente}" de `ZynstormECFPlatform-FrontEnd/app/clientes/page.tsx` algunos clientes no permiten copiar la clave.

### Causa raíz del punto 2

El cliente afectado **no tiene API Key**. No es un fallo del botón de copiar:

- `ClientController.Post` solo genera el `ApiKey` si `model.Email` no está vacío. `Put` nunca genera una. Un cliente creado sin email, o por otra vía, queda sin key.
- `ClientViewDto.ApiKey` se mapea con la primera `ApiKey` activa (`MappingProfiles`); sin ninguna activa queda `null`.
- El modal usa `value={apiKey || "No tiene API Key generada"}` dentro de un `<Input type="password">`: el texto de relleno se ve como 25 puntos (la frase tiene exactamente 25 caracteres), y los botones de ojo y copiar quedan `disabled`.

## Decisiones

| Tema | Decisión |
|---|---|
| Contenido del correo | API Key + Secret Key, igual que el correo de bienvenida (`IEmailService.SendApiKeyEmailAsync`). |
| Destinatario | Email registrado del cliente, precargado y **editable** en el modal. Funciona también para clientes sin email registrado. |
| Cliente sin API Key | Botón "Generar y enviar": crea el par de claves, lo guarda y lo envía en una sola acción. |

Fuera de alcance: rotar o revocar keys; generar la key automáticamente al editar un cliente que añade su email.

## Backend

### Endpoint

`POST v1/Client/guid/{guid}/api-key/send` en `ClientController`, siguiendo el patrón de `guid/{guid}/certificate-expiration/notify`.

Request: `ClientSendApiKeyDto { string Email }` (en `ZynstormECFPlatform.Dtos/ClientDtos.cs`).

Response 200: `ClientSendApiKeyResultDto { string Message, string ApiKey, bool Generated }`. Se devuelve la API Key para que el modal la muestre sin recargar la lista; el Secret Key **no** se devuelve.

### Lógica

1. Validar `Email` (obligatorio, formato válido, sin nombre para mostrar; se recorta con `Trim`) → 400 si no. Lo hace el servicio.
2. Buscar el cliente por `GuidId`. Un SA ve todos; los demás solo los de su `UserClients` (mismas reglas que `Put` y `certificate-expiration/notify`). No encontrado o sin acceso → 404. Lo hace el controlador.
3. Tomar la primera `ApiKey` con `StatusId == Active`.
   - **Existe:** `secretKey = encryptedService.DecryptString(apiKey.SecretKey)`. Si `SecretKey` es nulo o vacío, o no se puede descifrar → 422 con mensaje claro (no se regenera en silencio, porque cambiaría las credenciales de un cliente que ya integra). No se modifica la base de datos.
   - **No existe:** generar `Tools.GenerateSecureRandomString(32)` y `(64)`, insertar el `ApiKey` con `SecretKey = encryptedService.EncryptString(secretKey)` y `StatusId = Active`, dentro de `unitOfWork.ExecuteInTransactionAsync`, igual que `Post`. `Generated = true`.
4. Después de confirmar la transacción, `emailService.SendApiKeyEmailAsync(email, apiKey, secretKey)`. Si el envío falla → 503 con mensaje; si la key se acababa de generar, queda guardada y el reintento la reenvía en vez de duplicarla.
5. No se registran en logs la API Key ni el Secret Key.

### Estructura para pruebas

La lógica va en un servicio pequeño: `IClientApiKeyService.SendAsync(int clientId, string email, CancellationToken)` / `ClientApiKeyService` en `ZynstormECFPlatform.Services/Credentials`, dependiendo de `IApiKeyService`, `IEncryptedService`, `IEmailService`, `IUnitOfWork`. El servicio valida el email (paso 1), busca la key activa con `GetNoTrackingByAsync` (mockeable, a diferencia de `Table` + EF async) y ejecuta los pasos 3–4. El controlador solo comprueba el acceso al cliente, delega y traduce el resultado a HTTP. Así se prueba sin HTTP.

Las pruebas necesitan dobles de interfaces grandes (`IApiKeyService` hereda `IRepository<ApiKey>`), así que se añade el paquete `Moq` al proyecto `ZynstormECFPlatform.Tests`.

## Frontend

`ZynstormECFPlatform-FrontEnd`:

- `services/client.service.ts`: `sendClientApiKey(guidId, email): Promise<{ message; apiKey; generated }>` con `post`.
- `app/clientes/page.tsx`, modal de API Key:
  - **Con key:** campo de clave (ver/copiar) como ahora; debajo, campo de email (precargado con `client.email`) y botón "Enviar por correo".
  - **Sin key:** se elimina el campo de puntos; un aviso "Este cliente aún no tiene API Key", el mismo campo de email y el botón "Generar y enviar". Al terminar con éxito, el modal pasa al estado "con key".
  - Tras éxito se actualiza `apiKey` en `selectedClientForApiKey` y en la lista `clients`, y se muestra un toast.
  - Mientras se envía, botones deshabilitados y estado de carga. Errores del backend se muestran en un toast.
  - `handleCopyApiKey` pasa a `async`, espera `navigator.clipboard.writeText` y avisa si falla, en vez de marcar "copiado" siempre.
  - Validación de email en cliente antes de llamar.

## Pruebas

xUnit en `ZynstormECFPlatform.Tests` sobre `ClientApiKeyService` (doubles de `IApiKeyService`, `IEncryptedService`, `IEmailService`):

- Con key activa: descifra, envía el correo con la key y el secreto descifrado, no inserta nada.
- Sin key activa: inserta una `ApiKey` con secreto cifrado y envía el correo con los mismos valores; `Generated = true`.
- `SecretKey` vacío en una key existente: error de dominio, no se envía correo.
- Fallo de `SendApiKeyEmailAsync`: la excepción se propaga; si se generó, la key queda insertada.

El controlador (validación de email, acceso por usuario) se verifica manualmente. El frontend no tiene tests; se verifica a mano en el navegador: cliente con key, cliente sin key, email inválido, fallo de envío.
