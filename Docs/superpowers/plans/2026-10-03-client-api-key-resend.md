# Reenvío / generación de API Key de clientes — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir enviar por correo la API Key + Secret Key de un cliente desde el modal "API Key de …", y generarlas en el acto cuando el cliente no tiene.

**Architecture:** Un servicio nuevo `ClientApiKeyService` (en `ZynstormECFPlatform.Services/Credentials`) contiene la lógica (validar email, reutilizar o generar el par de claves, enviar el correo). Un endpoint `POST v1/Client/guid/{guid}/api-key/send` en `ClientController` comprueba el acceso al cliente y traduce el resultado a HTTP. El modal del frontend usa ese endpoint.

**Tech Stack:** .NET 10 / EF Core / xUnit + Moq (backend, repo `ZynstormECFPlatform`); Next.js + React + TypeScript + shadcn/ui (frontend, repo `ZynstormECFPlatform-FrontEnd`).

Spec: `Docs/superpowers/specs/2026-10-03-client-api-key-resend-design.md`.

## Global Constraints

- Contenido del correo: API Key **y** Secret Key, reutilizando `IEmailService.SendApiKeyEmailAsync(string email, string apiKey, string secretKey)`.
- El Secret Key **nunca** se devuelve en la respuesta HTTP ni se escribe en logs. La API Key tampoco se escribe en logs.
- Una key existente nunca se regenera ni se modifica: si su `SecretKey` no se puede descifrar, error 422 y no se envía nada.
- Reglas de acceso: SA ve todos los clientes; el resto solo los de su `UserClients` (mismas que `NotifyCertificateExpiration`).
- Textos de usuario en español. Los endpoints responden `{ "message": "..." }` en los errores (el `fetchHandler` del frontend lee `message`).
- Base actual: `dotnet test ZynstormECFPlatform.Tests` pasa 150 pruebas; no puede quedar ninguna roja.
- Los commits van en el repo que corresponda (`ZynstormECFPlatform` y `ZynstormECFPlatform-FrontEnd`, ambos en la rama `Staging`). Mensajes en español con prefijo `feat(clientes):`, como el historial existente. Terminan con `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Solo se hacen commits si el usuario lo autoriza.

Todas las rutas del backend son relativas a `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`; las del frontend, a `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.

---

## File Structure

Backend:
- Create `ZynstormECFPlatform.Services/Credentials/ClientApiKeyService.cs` — tipos de resultado, interfaz y servicio.
- Create `ZynstormECFPlatform.Tests/Credentials/ClientApiKeyServiceTests.cs` — pruebas del servicio.
- Modify `ZynstormECFPlatform.Tests/ZynstormECFPlatform.Tests.csproj` — paquete Moq.
- Modify `ZynstormECFPlatform.Dtos/ClientDtos.cs` — DTO de petición y de respuesta.
- Modify `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` — registro DI.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` — endpoint nuevo.

Frontend:
- Modify `types/client.type.ts` — tipo `ClientApiKeySendResult`.
- Modify `services/client.service.ts` — `sendClientApiKey`.
- Modify `app/clientes/page.tsx` — estado, handlers y modal.

---

### Task 1: Servicio `ClientApiKeyService` con pruebas

**Files:**
- Create: `ZynstormECFPlatform.Services/Credentials/ClientApiKeyService.cs`
- Create: `ZynstormECFPlatform.Tests/Credentials/ClientApiKeyServiceTests.cs`
- Modify: `ZynstormECFPlatform.Tests/ZynstormECFPlatform.Tests.csproj`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` (junto a la línea 59)

**Interfaces:**
- Produces (namespace `ZynstormECFPlatform.Services.Credentials`):
  - `enum ClientApiKeySendOutcome { Sent, InvalidEmail, SecretUnavailable }`
  - `sealed record ClientApiKeySendResult(ClientApiKeySendOutcome Outcome, string? ApiKey, bool Generated, string? Error)`
  - `interface IClientApiKeyService { Task<ClientApiKeySendResult> SendAsync(int clientId, string email, CancellationToken cancellationToken = default); }`
  - `class ClientApiKeyService(IApiKeyService, IEncryptedService, IEmailService, IUnitOfWork) : IClientApiKeyService`

- [ ] **Step 1: Añadir Moq al proyecto de pruebas**

En `ZynstormECFPlatform.Tests/ZynstormECFPlatform.Tests.csproj`, dentro del `ItemGroup` de `PackageReference`, añadir tras la línea de `Microsoft.NET.Test.Sdk`:

```xml
    <PackageReference Include="Moq" Version="4.20.72" />
```

- [ ] **Step 2: Escribir las pruebas (fallan porque el servicio no existe)**

Crear `ZynstormECFPlatform.Tests/Credentials/ClientApiKeyServiceTests.cs`:

```csharp
using System.Linq.Expressions;
using Moq;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Credentials;

namespace ZynstormECFPlatform.Tests.Credentials;

public class ClientApiKeyServiceTests
{
    private const int ClientId = 7;

    private readonly Mock<IApiKeyService> _apiKeys = new();
    private readonly Mock<IEncryptedService> _encryption = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<string> _calls = [];
    private ApiKey? _inserted;

    private ClientApiKeyService CreateService(ApiKey? existing)
    {
        _apiKeys
            .Setup(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<ApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _apiKeys
            .Setup(s => s.InsertAsync(It.IsAny<ApiKey>()))
            .Returns((ApiKey key) =>
            {
                _calls.Add("insert");
                _inserted = key;
                return Task.FromResult<ApiKey?>(key);
            });

        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, CancellationToken token) => operation(token));

        _email
            .Setup(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() =>
            {
                _calls.Add("email");
                return Task.CompletedTask;
            });

        return new ClientApiKeyService(_apiKeys.Object, _encryption.Object, _email.Object, _unitOfWork.Object);
    }

    private static ApiKey ExistingKey(string? secretKey = "CIPHER") => new()
    {
        ApiKeyId = 1,
        ClientId = ClientId,
        Apikey = "EXISTING-API-KEY",
        SecretKey = secretKey,
        StatusId = (int)StatusEnum.Active
    };

    [Fact]
    public async Task ExistingKey_ResendsDecryptedSecret_WithoutTouchingTheDatabase()
    {
        var service = CreateService(ExistingKey());
        _encryption.Setup(e => e.DecryptString("CIPHER")).Returns("PLAIN-SECRET");

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.Sent, result.Outcome);
        Assert.Equal("EXISTING-API-KEY", result.ApiKey);
        Assert.False(result.Generated);
        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", "EXISTING-API-KEY", "PLAIN-SECRET"), Times.Once);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoKey_GeneratesStoresEncryptedSecretAndSendsTheSameValues()
    {
        var service = CreateService(existing: null);
        string? plainSecret = null;
        _encryption
            .Setup(e => e.EncryptString(It.IsAny<string>()))
            .Returns((string plain) =>
            {
                plainSecret = plain;
                return "ENCRYPTED";
            });

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.Sent, result.Outcome);
        Assert.True(result.Generated);
        Assert.NotNull(_inserted);
        Assert.Equal(ClientId, _inserted!.ClientId);
        Assert.Equal((int)StatusEnum.Active, _inserted.StatusId);
        Assert.Equal("ENCRYPTED", _inserted.SecretKey);
        Assert.Equal(32, _inserted.Apikey.Length);
        Assert.Equal(64, plainSecret!.Length);
        Assert.Equal(_inserted.Apikey, result.ApiKey);
        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", _inserted.Apikey, plainSecret), Times.Once);
    }

    [Fact]
    public async Task NoKey_InsertsBeforeSendingTheEmail()
    {
        var service = CreateService(existing: null);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns("ENCRYPTED");

        await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(["insert", "email"], _calls);
    }

    [Fact]
    public async Task NoKey_WhenEmailFails_ErrorPropagatesButKeyStaysStored()
    {
        var service = CreateService(existing: null);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns("ENCRYPTED");
        _email
            .Setup(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp caído"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync(ClientId, "cliente@empresa.com"));

        Assert.NotNull(_inserted);
    }

    [Fact]
    public async Task NoKey_WhenEncryptionFails_ThrowsAndStoresNothing()
    {
        var service = CreateService(existing: null);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns(string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync(ClientId, "cliente@empresa.com"));

        Assert.Null(_inserted);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistingKeyWithoutSecret_ReportsSecretUnavailable_AndSendsNothing(string? storedSecret)
    {
        var service = CreateService(ExistingKey(storedSecret));

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.SecretUnavailable, result.Outcome);
        Assert.NotNull(result.Error);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
    }

    [Fact]
    public async Task ExistingKeyThatCannotBeDecrypted_ReportsSecretUnavailable_AndDoesNotRegenerate()
    {
        var service = CreateService(ExistingKey());
        _encryption.Setup(e => e.DecryptString("CIPHER")).Returns(string.Empty);

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.SecretUnavailable, result.Outcome);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-un-correo")]
    [InlineData("sin-dominio@")]
    [InlineData("a@sinpunto")]
    [InlineData("Juan Perez <juan@empresa.com>")]
    public async Task InvalidEmail_IsRejected_BeforeAnyWork(string email)
    {
        var service = CreateService(ExistingKey());

        var result = await service.SendAsync(ClientId, email);

        Assert.Equal(ClientApiKeySendOutcome.InvalidEmail, result.Outcome);
        Assert.NotNull(result.Error);
        _apiKeys.Verify(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<ApiKey, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Email_IsTrimmedBeforeSending()
    {
        var service = CreateService(ExistingKey());
        _encryption.Setup(e => e.DecryptString("CIPHER")).Returns("PLAIN-SECRET");

        await service.SendAsync(ClientId, "  cliente@empresa.com  ");

        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", "EXISTING-API-KEY", "PLAIN-SECRET"), Times.Once);
    }
}
```

- [ ] **Step 3: Ejecutar las pruebas y comprobar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests --nologo -v q`
Expected: error de compilación `CS0234`/`CS0246` por `ZynstormECFPlatform.Services.Credentials` / `ClientApiKeyService` inexistentes (el paquete Moq se restaura sin problema).

- [ ] **Step 4: Implementar el servicio**

Crear `ZynstormECFPlatform.Services/Credentials/ClientApiKeyService.cs`:

```csharp
using System.Net.Mail;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Credentials;

public enum ClientApiKeySendOutcome
{
    Sent,
    InvalidEmail,
    SecretUnavailable
}

public sealed record ClientApiKeySendResult(ClientApiKeySendOutcome Outcome, string? ApiKey, bool Generated, string? Error)
{
    public static ClientApiKeySendResult Sent(string apiKey, bool generated) =>
        new(ClientApiKeySendOutcome.Sent, apiKey, generated, null);

    public static ClientApiKeySendResult InvalidEmail(string error) =>
        new(ClientApiKeySendOutcome.InvalidEmail, null, false, error);

    public static ClientApiKeySendResult SecretUnavailable(string error) =>
        new(ClientApiKeySendOutcome.SecretUnavailable, null, false, error);
}

public interface IClientApiKeyService
{
    /// <summary>
    /// Envía por correo la API Key y el Secret Key de un cliente. Si el cliente no tiene una key activa,
    /// la genera y la guarda antes de enviarla. Los errores de envío (SMTP) se propagan como excepción.
    /// </summary>
    Task<ClientApiKeySendResult> SendAsync(int clientId, string email, CancellationToken cancellationToken = default);
}

public class ClientApiKeyService(
    IApiKeyService apiKeyService,
    IEncryptedService encryptedService,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : IClientApiKeyService
{
    private const string InvalidEmailMessage = "El correo electrónico no es válido.";
    private const string SecretUnavailableMessage =
        "La API Key existente no tiene un Secret Key recuperable. No se regeneró para no romper la integración del cliente; contacte a soporte.";

    public async Task<ClientApiKeySendResult> SendAsync(int clientId, string email, CancellationToken cancellationToken = default)
    {
        var recipient = email?.Trim();
        if (!IsValidEmail(recipient))
            return ClientApiKeySendResult.InvalidEmail(InvalidEmailMessage);

        var existing = await apiKeyService.GetNoTrackingByAsync(
            k => k.ClientId == clientId && k.StatusId == (int)StatusEnum.Active, cancellationToken);

        string apiKey;
        string secretKey;
        bool generated;

        if (existing != null)
        {
            secretKey = string.IsNullOrWhiteSpace(existing.SecretKey)
                ? string.Empty
                : encryptedService.DecryptString(existing.SecretKey);

            if (string.IsNullOrEmpty(secretKey))
                return ClientApiKeySendResult.SecretUnavailable(SecretUnavailableMessage);

            apiKey = existing.Apikey;
            generated = false;
        }
        else
        {
            apiKey = Tools.GenerateSecureRandomString(32);
            secretKey = Tools.GenerateSecureRandomString(64);

            var encryptedSecret = encryptedService.EncryptString(secretKey);
            if (string.IsNullOrEmpty(encryptedSecret))
                throw new InvalidOperationException("No se pudo cifrar el Secret Key del cliente.");

            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await apiKeyService.InsertAsync(new ApiKey
                {
                    ClientId = clientId,
                    Apikey = apiKey,
                    SecretKey = encryptedSecret,
                    StatusId = (int)StatusEnum.Active
                });
            }, cancellationToken);

            generated = true;
        }

        await emailService.SendApiKeyEmailAsync(recipient!, apiKey, secretKey);

        return ClientApiKeySendResult.Sent(apiKey, generated);
    }

    private static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && MailAddress.TryCreate(value, out var address)
        && address.Address == value
        && address.Host.Contains('.');
}
```

- [ ] **Step 5: Registrar el servicio en DI**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, después de la línea `services.AddTransient<Billing.IClientPaymentRegistrationService, Billing.ClientPaymentRegistrationService>();` añadir:

```csharp
        services.AddTransient<Credentials.IClientApiKeyService, Credentials.ClientApiKeyService>();
```

- [ ] **Step 6: Ejecutar las pruebas y comprobar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests --nologo -v q`
Expected: `Passed! - Failed: 0, Passed: 166` (150 anteriores + 16 nuevas: 7 `[Fact]` y 9 casos de `[Theory]`).

Si `ExistingKeyWithoutSecret_...` con `"   "` falla porque `DecryptString` recibe espacios, revisar que el servicio use `IsNullOrWhiteSpace` (ya lo hace). Si alguna prueba de email inválido falla, comprobar qué devuelve `MailAddress.TryCreate` para esa entrada y ajustar `IsValidEmail`, no la prueba.

- [ ] **Step 7: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
git add Docs/superpowers ZynstormECFPlatform.Services/Credentials ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs ZynstormECFPlatform.Tests
git commit -m "feat(clientes): servicio para reenviar o generar la API Key de un cliente"
```

---

### Task 2: Endpoint `POST v1/Client/guid/{guid}/api-key/send`

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/ClientDtos.cs` (añadir al final del archivo, dentro del namespace `ZynstormECFPlatform.Dtos`)
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` (using, constructor, endpoint nuevo tras `NotifyCertificateExpiration`, ~línea 615)

**Interfaces:**
- Consumes: `IClientApiKeyService.SendAsync(int clientId, string email, CancellationToken)` y `ClientApiKeySendOutcome` de la Task 1.
- Produces: `POST v1/Client/guid/{guid}/api-key/send`, body `ClientSendApiKeyDto { Email }`, respuesta 200 `ClientSendApiKeyResultDto { Message, ApiKey, Generated }` (serializado en camelCase: `message`, `apiKey`, `generated`).

- [ ] **Step 1: Añadir los DTOs**

Al final de `ZynstormECFPlatform.Dtos/ClientDtos.cs` añadir:

```csharp
public class ClientSendApiKeyDto
{
    [Required]
    public string Email { get; set; } = null!;
}

public class ClientSendApiKeyResultDto
{
    public string Message { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public bool Generated { get; set; }
}
```

- [ ] **Step 2: Inyectar el servicio en el controlador**

En `ClientController.cs`:

1. Añadir el using junto a los demás `ZynstormECFPlatform.Services.*`:

```csharp
using ZynstormECFPlatform.Services.Credentials;
```

2. En el constructor primario, añadir el parámetro justo después de `IClientPaymentRegistrationService paymentRegistrationService,`:

```csharp
        IClientApiKeyService clientApiKeyService,
```

- [ ] **Step 3: Añadir el endpoint**

Después del método `NotifyCertificateExpiration` (termina en la llave que sigue a `"No se pudo enviar el aviso de vencimiento."`) y antes de `GetMonthlyUsage`, insertar:

```csharp
        [HttpPost]
        [Route("guid/{guid}/api-key/send", Order = 1)]
        public async Task<IActionResult> SendApiKey(string guid, [FromBody] ClientSendApiKeyDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Repository.Table.AsNoTracking().Where(c => c.GuidId == guid);
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var client = await query.FirstOrDefaultAsync(cancellationToken);
                if (client == null)
                    return NotFound(new { message = "No se encontró el cliente." });

                var result = await clientApiKeyService.SendAsync(client.ClientId, dto.Email, cancellationToken);

                return result.Outcome switch
                {
                    ClientApiKeySendOutcome.Sent => Ok(new ClientSendApiKeyResultDto
                    {
                        Message = result.Generated
                            ? $"Se generó la API Key y se envió a {dto.Email.Trim()}."
                            : $"API Key enviada a {dto.Email.Trim()}.",
                        ApiKey = result.ApiKey!,
                        Generated = result.Generated
                    }),
                    ClientApiKeySendOutcome.InvalidEmail => BadRequest(new { message = result.Error }),
                    _ => UnprocessableEntity(new { message = result.Error })
                };
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error enviando la API Key del cliente {Guid}", guid);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "No se pudo enviar el correo. Si la API Key se acababa de generar, quedó guardada; intente nuevamente para reenviarla."
                });
            }
        }
```

- [ ] **Step 4: Compilar la API**

Run: `dotnet build ZynstormECFPlatform.Web.Api --nologo -v q`
Expected: `Build succeeded` con `0 Error(s)` (los warnings CS86xx existentes son normales).

- [ ] **Step 5: Ejecutar toda la suite**

Run: `dotnet test ZynstormECFPlatform.Tests --nologo -v q`
Expected: `Failed: 0`.

- [ ] **Step 6: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Web.Api
git commit -m "feat(clientes): endpoint para enviar o generar la API Key por correo"
```

---

### Task 3: Frontend — servicio, tipo y modal de API Key

**Files:**
- Modify: `types/client.type.ts` (añadir tipo exportado al final)
- Modify: `services/client.service.ts`
- Modify: `app/clientes/page.tsx` (estado ~línea 283, `openApiKeyModal`/`handleCopyApiKey` ~452-464, modal ~1445-1514)

**Interfaces:**
- Consumes: `POST v1/Client/guid/{guid}/api-key/send` de la Task 2, respuesta `{ message, apiKey, generated }`.
- Produces: `sendClientApiKey(guidId: string, email: string): Promise<ClientApiKeySendResult>`.

- [ ] **Step 1: Añadir el tipo**

Al final de `types/client.type.ts`:

```ts
export interface ClientApiKeySendResult {
  message: string
  apiKey: string
  generated: boolean
}
```

- [ ] **Step 2: Añadir la función de servicio**

En `services/client.service.ts`, cambiar el import de tipos a:

```ts
import { Client, ClientApiKeySendResult, ClientCreate, ClientUpdate, PaginatedResponse } from "@/types/client.type"
```

y añadir después de `notifyCertificateExpiration`:

```ts
export const sendClientApiKey = async (guidId: string, email: string): Promise<ClientApiKeySendResult> => {
  return await post<ClientApiKeySendResult>(`${CLIENT_URL}/guid/${guidId}/api-key/send`, { email })
}
```

- [ ] **Step 3: Importar la función en la página**

En `app/clientes/page.tsx` cambiar la línea 64:

```ts
import { notifyCertificateExpiration } from "@/services/client.service"
```

por:

```ts
import { notifyCertificateExpiration, sendClientApiKey } from "@/services/client.service"
```

(`Send` ya está importado de `lucide-react` en la línea ~51.)

- [ ] **Step 4: Añadir el estado del envío**

Debajo de `const [copied, setCopied] = useState(false)` (línea ~284) añadir:

```ts
  const [apiKeyEmail, setApiKeyEmail] = useState("")
  const [isSendingApiKey, setIsSendingApiKey] = useState(false)
  const [apiKeyMessage, setApiKeyMessage] = useState<{ type: "success" | "error"; text: string } | null>(null)
```

- [ ] **Step 5: Reemplazar `openApiKeyModal` y `handleCopyApiKey`, y añadir `handleSendApiKey`**

Sustituir el bloque actual (líneas ~452-464):

```ts
  const openApiKeyModal = (client: Client) => {
    setSelectedClientForApiKey(client)
    setShowApiKey(false)
    setCopied(false)
    setIsApiKeyModalOpen(true)
  }

  const handleCopyApiKey = () => {
    if (!selectedClientForApiKey?.apiKey) return
    navigator.clipboard.writeText(selectedClientForApiKey.apiKey)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }
```

por:

```ts
  const openApiKeyModal = (client: Client) => {
    setSelectedClientForApiKey(client)
    setShowApiKey(false)
    setCopied(false)
    setApiKeyEmail(client.email ?? "")
    setApiKeyMessage(null)
    setIsSendingApiKey(false)
    setIsApiKeyModalOpen(true)
  }

  const handleCopyApiKey = async () => {
    if (!selectedClientForApiKey?.apiKey) return
    try {
      await navigator.clipboard.writeText(selectedClientForApiKey.apiKey)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      setApiKeyMessage({
        type: "error",
        text: "No se pudo copiar al portapapeles. Muestra la clave con el ojo y cópiala manualmente.",
      })
    }
  }

  const isValidEmail = (value: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)

  const handleSendApiKey = async () => {
    if (!selectedClientForApiKey) return

    const email = apiKeyEmail.trim()
    if (!isValidEmail(email)) {
      setApiKeyMessage({ type: "error", text: "Ingresa un correo electrónico válido." })
      return
    }

    setIsSendingApiKey(true)
    setApiKeyMessage(null)
    try {
      const result = await sendClientApiKey(selectedClientForApiKey.guidId, email)
      setSelectedClientForApiKey({ ...selectedClientForApiKey, apiKey: result.apiKey })
      setApiKeyMessage({ type: "success", text: result.message })
      if (result.generated) fetchClients()
    } catch (err) {
      setApiKeyMessage({
        type: "error",
        text: err instanceof Error ? err.message : "No se pudo enviar la API Key.",
      })
    } finally {
      setIsSendingApiKey(false)
    }
  }
```

- [ ] **Step 6: Reemplazar el cuerpo y el pie del modal**

Sustituir, dentro de `{/* Modal de API Key */}`, desde `<div className="space-y-4 py-4">` hasta el cierre de `</DialogFooter>` (líneas ~1458-1512) por:

```tsx
            <div className="space-y-4 py-4">
              {selectedClientForApiKey?.apiKey ? (
                <div className="space-y-2">
                  <Label htmlFor="api-key">Clave de Acceso (API Key)</Label>
                  <div className="relative flex items-center gap-2">
                    <Input
                      id="api-key"
                      type={showApiKey ? "text" : "password"}
                      value={selectedClientForApiKey.apiKey}
                      readOnly
                      className="font-mono text-sm pr-20 h-11"
                    />
                    <div className="absolute right-2 flex items-center gap-1">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 hover:bg-transparent"
                        onClick={() => setShowApiKey(!showApiKey)}
                      >
                        {showApiKey ? (
                          <EyeOff className="h-4 w-4 text-muted-foreground" />
                        ) : (
                          <Eye className="h-4 w-4 text-muted-foreground" />
                        )}
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="h-8 w-8"
                        onClick={handleCopyApiKey}
                      >
                        {copied ? (
                          <Check className="h-4 w-4 text-green-600" />
                        ) : (
                          <Copy className="h-4 w-4" />
                        )}
                      </Button>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
                  Este cliente aún no tiene API Key. Se generará junto con su Secret Key y se enviará al correo indicado.
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="api-key-email">Enviar credenciales a</Label>
                <Input
                  id="api-key-email"
                  type="email"
                  value={apiKeyEmail}
                  onChange={(e) => setApiKeyEmail(e.target.value)}
                  placeholder="correo@cliente.com"
                  disabled={isSendingApiKey}
                />
              </div>

              {apiKeyMessage && (
                <div
                  className={`rounded-md border p-3 text-sm ${
                    apiKeyMessage.type === "success"
                      ? "border-green-200 bg-green-50 text-green-800"
                      : "border-red-200 bg-red-50 text-red-800"
                  }`}
                >
                  {apiKeyMessage.text}
                </div>
              )}
            </div>

            <DialogFooter className="gap-2 sm:gap-0">
              <Button
                type="button"
                variant="outline"
                className="w-full sm:w-auto"
                onClick={() => setIsApiKeyModalOpen(false)}
                disabled={isSendingApiKey}
              >
                Cerrar
              </Button>
              <Button
                type="button"
                className="w-full sm:w-auto"
                onClick={handleSendApiKey}
                disabled={isSendingApiKey || !apiKeyEmail.trim()}
              >
                <Send className="mr-2 h-4 w-4" />
                {isSendingApiKey
                  ? "Enviando..."
                  : selectedClientForApiKey?.apiKey
                    ? "Enviar por correo"
                    : "Generar y enviar"}
              </Button>
            </DialogFooter>
```

- [ ] **Step 7: Comprobar tipos y lint**

Run (en `ZynstormECFPlatform-FrontEnd`): `npx tsc --noEmit`
Expected: sin errores nuevos en `app/clientes/page.tsx`, `services/client.service.ts` ni `types/client.type.ts` (si el proyecto ya tenía errores previos en otros archivos, compararlos con `git stash` y no introducir ninguno nuevo).

Run: `npx eslint app/clientes/page.tsx services/client.service.ts types/client.type.ts`
Expected: sin errores nuevos.

- [ ] **Step 8: Verificación manual en el navegador**

Con la API y el frontend en marcha (`npm run dev`), en la página Clientes → menú del cliente → "API Key":

1. **Cliente sin key** (como SURTIDORA DANIA NUÑEZ): aparece el aviso ámbar en vez de los puntos; el botón dice "Generar y enviar"; el email viene precargado. Enviar → mensaje verde, el modal pasa a mostrar la key con ojo y copiar, y copiar funciona (pegar en un campo de texto para comprobarlo). El cliente recibe un correo con API Key y Secret Key.
2. **Cliente con key:** botón "Enviar por correo"; el correo llega con la misma API Key y el Secret Key; la key mostrada no cambia.
3. **Email inválido** (`abc`): mensaje rojo "Ingresa un correo electrónico válido.", no hay petición.
4. **Fallo de envío** (apagar SMTP o usar credencial inválida): mensaje rojo del backend; al reintentar con el SMTP bien, reenvía la misma key en vez de crear otra.
5. Un usuario no SA no puede enviar la key de un cliente que no es suyo (404).

Si no se puede levantar la API local, indicarlo explícitamente en el informe final en vez de dar la verificación por hecha.

- [ ] **Step 9: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
git add types/client.type.ts services/client.service.ts app/clientes/page.tsx
git commit -m "feat(clientes): enviar o generar la API Key por correo desde el modal"
```

---

## Self-Review

- **Cobertura del spec:** endpoint y DTOs (Task 2), reenvío con descifrado sin tocar BD (Task 1, prueba 1), generación atómica con transacción (Task 1, pruebas 2-4), 422 si el secreto no es recuperable (Task 1, pruebas 6-7; Task 2 `UnprocessableEntity`), email editable y validado (Task 1 `IsValidEmail`; Task 3 paso 5/6), 404 por acceso (Task 2), modal con/sin key y "Generar y enviar" (Task 3), `handleCopyApiKey` async con aviso de fallo (Task 3 paso 5), Secret Key no devuelto (Task 2 DTO sin ese campo), pruebas xUnit (Task 1), verificación manual del front (Task 3 paso 8).
- **Sin placeholders:** todos los pasos de código llevan el código completo.
- **Consistencia de tipos:** `IClientApiKeyService.SendAsync(int, string, CancellationToken)`, `ClientApiKeySendOutcome.{Sent, InvalidEmail, SecretUnavailable}` y `ClientApiKeySendResult(Outcome, ApiKey, Generated, Error)` se usan igual en las Tasks 1 y 2; el JSON `{ message, apiKey, generated }` coincide con `ClientApiKeySendResult` del frontend.
- **Riesgo conocido:** si hubiera varias keys activas para un cliente, el modal (mapeo `FirstOrDefault` sobre `ApiKeys`) y `GetNoTrackingByAsync` podrían escoger distinta. No hay evidencia de que ocurra (`Post` crea una sola y no hay otro punto de creación); si se detecta, unificar el criterio de orden en ambos sitios.
