# Cuenta de facturación con pool — Fase 3: permisos, usuarios y aislamiento por cliente

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el Admin revendedor opere solo lo suyo: sus clientes nacen dentro de su cuenta, solo asigna a sus usuarios clientes que él ve, consulta su pool y sus pagos en solo lectura y fija el cupo informativo; y que un Admin ya no pueda tocar clientes ni usuarios de otro.

**Architecture:** El acceso a un cliente sigue siendo `UserClient` (la regla que ya usan las demás pantallas). Se agrega una guía reutilizable `IClientAccessService` para los endpoints que hoy no comprueban pertenencia. Al asignar un cliente a una cuenta, el dueño recibe su `UserClient`, así todo el sistema (listados, documentos, dashboard) lo ve sin cambios. La lectura de la cuenta se extrae del controlador del SA a `BillingAccountViewService` y la usa también el controlador del dueño.

**Tech Stack:** .NET 10, EF Core + Npgsql, xUnit + Moq.

Spec: `Docs/superpowers/specs/2026-11-11-cuenta-facturacion-pool-design.md` (§3). Fases anteriores: `…-fase1-datos-consumo.md`, `…-fase2-cobro.md`.

## Global Constraints

- **No toques ninguna base de datos.** El `DefaultConnection` activo de `appsettings.Development.json` apunta a producción. Esta fase no necesita migraciones ni conexión; solo compilar y probar.
- SA siempre tiene acceso total. Un Admin solo accede a los clientes que tiene en `UserClient`. Un Normal solo a los suyos.
- El cupo (`Client.QuotaDocuments`) es informativo: nunca bloquea la emisión.
- Al soltar un cliente de la cuenta **no** se quita el `UserClient` del dueño (puede haberlo creado él); queda para el SA quitarlo desde el usuario.
- Un Admin que edita a un usuario solo reemplaza las asignaciones de clientes que él mismo ve; no borra las que otro le asignó a ese usuario.
- Los controladores y servicios que dependen de `IQueryable` de EF no se prueban con Moq; su lógica está en funciones puras con pruebas. Los servicios con `GetNoTrackingByAsync`/`GetManyByAsync` sí se prueban con Moq evaluando el predicado real.
- Fuera de esta fase: pantallas (fase 4); `CertificationController` (hoy no tiene `[Authorize]`, ver Task 8) y `FeController` (anónimo por diseño).
- Mensajes de commit en español, con las líneas `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` y `Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V` al final. Cada commit imprime la rama con `git branch --show-current`; confirma la rama con el usuario antes del Task 1 (el repo estaba en `dev`).

## Estructura de archivos

| Archivo | Acción | Responsabilidad |
|---|---|---|
| `ZynstormECFPlatform.Services/Users/ClientAccessService.cs` | Crear | Guía de acceso a clientes (SA o `UserClient`) |
| `ZynstormECFPlatform.Services/Users/UserClientAssignmentPolicy.cs` | Crear | Reglas puras de asignación de clientes a usuarios |
| `ZynstormECFPlatform.Services/Billing/ClientCreationRules.cs` | Crear | Regla pura del plan de un cliente nuevo |
| `ZynstormECFPlatform.Services/Billing/BillingAccountPolicy.cs` | Modificar | `ValidateQuota` |
| `ZynstormECFPlatform.Services/Billing/IBillingAccountViewService.cs`, `BillingAccountViewService.cs` | Crear | Vistas de cuentas y cupo (extraído del controlador) |
| `ZynstormECFPlatform.Dtos/BillingAccountDtos.cs` | Modificar | `ClientQuotaDto` |
| `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs` | Reescribir | Delega en el servicio; sincroniza `UserClient`; cupo |
| `ZynstormECFPlatform.Web.Api/Controllers/MyBillingAccountController.cs` | Crear | Cuenta del Admin dueño, solo lectura + cupo |
| `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` | Modificar | Cliente nuevo nace en la cuenta del dueño |
| `ZynstormECFPlatform.Web.Api/Controllers/UserController.cs` | Modificar | Validaciones de `POST/PUT/DELETE` |
| `ZynstormECFPlatform.Web.Api/Controllers/ClientCertificateController.cs`, `RiFormatController.cs` | Modificar | Aislamiento por cliente |
| `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` | Modificar | DI |
| `ZynstormECFPlatform.Tests/Users/*`, `Billing/*` | Crear | Pruebas |

Todos los comandos se corren desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`.

---

### Task 1: Guía de acceso a clientes

**Files:**
- Create: `ZynstormECFPlatform.Services/Users/ClientAccessService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`
- Test: `ZynstormECFPlatform.Tests/Users/ClientAccessServiceTests.cs`

**Interfaces:**
- Produces `IClientAccessService`:
  - `Task<bool> CanAccessClientAsync(ClaimsPrincipal user, int clientId, CancellationToken ct = default)`
  - `Task<bool> CanAccessClientAsync(ClaimsPrincipal user, string clientGuid, CancellationToken ct = default)` — `false` si el cliente no existe (salvo SA, que devuelve `true` sin consultar; el controlador resuelve el "no existe")
  - `Task<HashSet<int>?> GetAccessibleClientIdsAsync(ClaimsPrincipal user, CancellationToken ct = default)` — `null` = sin restricción (SA)

- [ ] **Step 1: Escribir las pruebas (fallan: no existe el servicio)**

`ZynstormECFPlatform.Tests/Users/ClientAccessServiceTests.cs`:

```csharp
using System.Linq.Expressions;
using System.Security.Claims;
using Moq;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Users;

namespace ZynstormECFPlatform.Tests.Users;

public class ClientAccessServiceTests
{
    private readonly List<UserClient> _links = [];
    private readonly List<Client> _clients = [];
    private readonly Mock<IUserClientService> _userClients = new();
    private readonly Mock<IClientService> _clientService = new();

    public ClientAccessServiceTests()
    {
        // Los predicados reales se evalúan contra las listas, así la prueba comprueba la regla y no el mock.
        _userClients
            .Setup(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<UserClient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<UserClient, bool>> p, CancellationToken _) => _links.FirstOrDefault(p.Compile()));

        _userClients
            .Setup(s => s.GetManyByAsync(It.IsAny<Expression<Func<UserClient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<UserClient, bool>> p, CancellationToken _) =>
                (IEnumerable<UserClient>)_links.Where(p.Compile()).ToList());

        _clientService
            .Setup(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<Client, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Client, bool>> p, CancellationToken _) => _clients.FirstOrDefault(p.Compile()));
    }

    private ClientAccessService CreateService() => new(_userClients.Object, _clientService.Object);

    private static ClaimsPrincipal User(string role, string? userId = "u1")
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task SA_CanAccessAnyClient_WithoutQueryingAssignments()
    {
        var allowed = await CreateService().CanAccessClientAsync(User("SA"), 99);

        Assert.True(allowed);
        _userClients.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_CanAccessOnlyAssignedClients()
    {
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });
        var service = CreateService();

        Assert.True(await service.CanAccessClientAsync(User("Admin"), 5));
        Assert.False(await service.CanAccessClientAsync(User("Admin"), 6));
    }

    [Fact]
    public async Task Admin_DoesNotInheritAnotherUsersAssignments()
    {
        _links.Add(new UserClient { UserId = "other", ClientId = 5 });

        Assert.False(await CreateService().CanAccessClientAsync(User("Admin"), 5));
    }

    [Fact]
    public async Task UserWithoutId_HasNoAccess()
    {
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });

        Assert.False(await CreateService().CanAccessClientAsync(User("Admin", userId: null), 5));
    }

    [Fact]
    public async Task ByGuid_ResolvesTheClientAndChecksTheAssignment()
    {
        _clients.Add(new Client { ClientId = 5, GuidId = "g-5" });
        _clients.Add(new Client { ClientId = 6, GuidId = "g-6" });
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });
        var service = CreateService();

        Assert.True(await service.CanAccessClientAsync(User("Admin"), "g-5"));
        Assert.False(await service.CanAccessClientAsync(User("Admin"), "g-6"));
        Assert.False(await service.CanAccessClientAsync(User("Admin"), "missing"));
    }

    [Fact]
    public async Task ByGuid_SA_IsAllowedWithoutLookingTheClientUp()
    {
        Assert.True(await CreateService().CanAccessClientAsync(User("SA"), "anything"));
        _clientService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AccessibleClientIds_AreNullForSA_AndTheAssignedOnesOtherwise()
    {
        _links.Add(new UserClient { UserId = "u1", ClientId = 5 });
        _links.Add(new UserClient { UserId = "u1", ClientId = 7 });
        _links.Add(new UserClient { UserId = "other", ClientId = 9 });
        var service = CreateService();

        Assert.Null(await service.GetAccessibleClientIdsAsync(User("SA")));
        Assert.Equal(new HashSet<int> { 5, 7 }, await service.GetAccessibleClientIdsAsync(User("Admin")));
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientAccessServiceTests" 2>&1 | grep -E " error " | sort -u | head -3`
Expected: error de compilación (`ClientAccessService` no existe).

- [ ] **Step 3: Implementar**

`ZynstormECFPlatform.Services/Users/ClientAccessService.cs`:

```csharp
using System.Security.Claims;
using ZynstormECFPlatform.Abstractions.DataServices;

namespace ZynstormECFPlatform.Services.Users;

public interface IClientAccessService
{
    /// <summary>SA accede a todo; el resto, solo a los clientes que tiene asignados (UserClient).</summary>
    Task<bool> CanAccessClientAsync(ClaimsPrincipal user, int clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual, por GUID del cliente. Un cliente que no existe no es accesible; SA devuelve true sin consultar
    /// y es el controlador quien resuelve el "no existe".
    /// </summary>
    Task<bool> CanAccessClientAsync(ClaimsPrincipal user, string clientGuid, CancellationToken cancellationToken = default);

    /// <summary>Ids de los clientes accesibles. Null = sin restricción (SA).</summary>
    Task<HashSet<int>?> GetAccessibleClientIdsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public class ClientAccessService(
    IUserClientService userClientService,
    IClientService clientService) : IClientAccessService
{
    private static bool IsSA(ClaimsPrincipal user) => user.IsInRole("SA");

    private static string? UserId(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task<bool> CanAccessClientAsync(ClaimsPrincipal user, int clientId, CancellationToken cancellationToken = default)
    {
        if (IsSA(user)) return true;

        var userId = UserId(user);
        if (string.IsNullOrEmpty(userId)) return false;

        var link = await userClientService.GetNoTrackingByAsync(
            x => x.UserId == userId && x.ClientId == clientId, cancellationToken);

        return link != null;
    }

    public async Task<bool> CanAccessClientAsync(ClaimsPrincipal user, string clientGuid, CancellationToken cancellationToken = default)
    {
        if (IsSA(user)) return true;

        var client = await clientService.GetNoTrackingByAsync(c => c.GuidId == clientGuid, cancellationToken);

        return client != null && await CanAccessClientAsync(user, client.ClientId, cancellationToken);
    }

    public async Task<HashSet<int>?> GetAccessibleClientIdsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (IsSA(user)) return null;

        var userId = UserId(user);
        if (string.IsNullOrEmpty(userId)) return [];

        var links = await userClientService.GetManyByAsync(x => x.UserId == userId, cancellationToken);

        return links.Select(x => x.ClientId).ToHashSet();
    }
}
```

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, reemplazar:

```csharp
        services.AddScoped<Users.IUserPageAccessManager, Users.UserPageAccessManager>();
```

por:

```csharp
        services.AddScoped<Users.IUserPageAccessManager, Users.UserPageAccessManager>();
        services.AddScoped<Users.IClientAccessService, Users.ClientAccessService>();
```

- [ ] **Step 4: Correr y verificar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientAccessServiceTests" 2>&1 | grep -E " error |Passed!|Failed" | sort -u`
Expected: `Passed!  - Failed: 0, Passed: 7`.

- [ ] **Step 5: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services ZynstormECFPlatform.Tests/Users/ClientAccessServiceTests.cs && git commit -q -m "$(cat <<'EOF'
Guía de acceso a clientes por usuario

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 2: El cliente nuevo nace en la cuenta del dueño

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/ClientCreationRules.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/ClientCreationRulesTests.cs`

**Interfaces:**
- Produces: `ClientCreationRules.ResolvePlanId(int? requestedPlanId, int? ownedAccountId)` → `int?` (null si el creador tiene cuenta: sus clientes no llevan plan propio).

- [ ] **Step 1: Prueba (falla: no existe `ClientCreationRules`)**

`ZynstormECFPlatform.Tests/Billing/ClientCreationRulesTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientCreationRulesTests
{
    [Fact]
    public void CreatorWithoutAccount_KeepsTheRequestedPlan() =>
        Assert.Equal(5, ClientCreationRules.ResolvePlanId(5, ownedAccountId: null));

    [Fact]
    public void CreatorWithoutAccount_AndNoPlan_StaysWithoutPlan() =>
        Assert.Null(ClientCreationRules.ResolvePlanId(null, ownedAccountId: null));

    [Fact]
    public void CreatorWithAccount_DropsTheRequestedPlan_BecauseTheClientBillsThroughTheAccount() =>
        Assert.Null(ClientCreationRules.ResolvePlanId(5, ownedAccountId: 3));
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientCreationRulesTests" 2>&1 | grep -E " error " | sort -u | head -3`
Expected: error de compilación.

- [ ] **Step 3: Implementar la regla**

`ZynstormECFPlatform.Services/Billing/ClientCreationRules.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Billing;

public static class ClientCreationRules
{
    /// <summary>
    /// Un usuario dueño de una cuenta crea clientes que facturan contra ella: no llevan plan propio,
    /// aunque el formulario lo envíe.
    /// </summary>
    public static int? ResolvePlanId(int? requestedPlanId, int? ownedAccountId) =>
        ownedAccountId.HasValue ? null : requestedPlanId;
}
```

- [ ] **Step 4: Aplicarla en `ClientController`**

En `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`:

1. Reemplazar `        IClientApiKeyService clientApiKeyService,` por:

```csharp
        IClientApiKeyService clientApiKeyService,
        IBillingAccountService billingAccountService,
```

2. Reemplazar:

```csharp
        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsSA => User.IsInRole("SA");
```

por:

```csharp
        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsSA => User.IsInRole("SA");

        /// <summary>Cuenta de facturación activa de la que el usuario actual es dueño, si tiene una.</summary>
        private async Task<int?> FindOwnedAccountIdAsync()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return null;

            var account = await billingAccountService.GetNoTrackingByAsync(
                a => a.OwnerUserId == userId && a.StatusId == (int)StatusEnum.Active);

            return account?.BillingAccountId;
        }
```

3. En `Post`, reemplazar:

```csharp
                var plan = await FindPlanAsync(dto.PlanId);
                if (dto.PlanId.HasValue && plan == null)
                    return BadRequest("El plan seleccionado no existe.");

                if (ValidatePaymentDates(dto) is string paymentDateError)
                    return BadRequest(paymentDateError);

                Client? model = null;
```

por:

```csharp
                var ownedAccountId = await FindOwnedAccountIdAsync();
                dto.PlanId = ClientCreationRules.ResolvePlanId(dto.PlanId, ownedAccountId);

                var plan = await FindPlanAsync(dto.PlanId);
                if (dto.PlanId.HasValue && plan == null)
                    return BadRequest("El plan seleccionado no existe.");

                if (ValidatePaymentDates(dto) is string paymentDateError)
                    return BadRequest(paymentDateError);

                Client? model = null;
```

y, dentro de la transacción de `Post`, reemplazar:

```csharp
                    model.CustomMonthlyFee = ClientPricing.ResolveCustomFee(IsSA, dto.CustomMonthlyFee, null);

                    model = await Repository.InsertAsync(model);
```

por:

```csharp
                    model.CustomMonthlyFee = ClientPricing.ResolveCustomFee(IsSA, dto.CustomMonthlyFee, null);
                    model.BillingAccountId = ownedAccountId;

                    model = await Repository.InsertAsync(model);
```

- [ ] **Step 5: Compilar y correr**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientCreationRulesTests" 2>&1 | grep -E "Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Passed!  - Failed: 0, Passed: 3`.

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services/Billing/ClientCreationRules.cs ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs ZynstormECFPlatform.Tests/Billing/ClientCreationRulesTests.cs && git commit -q -m "$(cat <<'EOF'
El cliente que crea el dueño de una cuenta nace dentro de la cuenta

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 3: `UserController` — asignación de clientes y alcance

**Files:**
- Create: `ZynstormECFPlatform.Services/Users/UserClientAssignmentPolicy.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/UserController.cs`
- Test: `ZynstormECFPlatform.Tests/Users/UserClientAssignmentPolicyTests.cs`

**Interfaces:**
- Produces:
  - `UserClientAssignmentPolicy.FindForbidden(IEnumerable<int>? requested, ISet<int>? allowed)` → `List<int>` (vacío si `allowed` es null = SA)
  - `UserClientAssignmentPolicy.ClientIdsToReplace(IEnumerable<int> existing, ISet<int>? allowed)` → `List<int>` (todas si `allowed` es null; si no, solo las que el que edita ve)
  - `UserClientAssignmentPolicy.ForbiddenMessage` (`string`)

- [ ] **Step 1: Pruebas (fallan: no existe la política)**

`ZynstormECFPlatform.Tests/Users/UserClientAssignmentPolicyTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Users;

namespace ZynstormECFPlatform.Tests.Users;

public class UserClientAssignmentPolicyTests
{
    [Fact]
    public void SA_AllowedIsNull_NothingIsForbidden() =>
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden([1, 2, 3], allowed: null));

    [Fact]
    public void Admin_CannotAssignClientsHeDoesNotSee()
    {
        var forbidden = UserClientAssignmentPolicy.FindForbidden([1, 2, 9], new HashSet<int> { 1, 2 });

        Assert.Equal([9], forbidden);
    }

    [Fact]
    public void Admin_CanAssignAnySubsetOfHisClients() =>
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden([2], new HashSet<int> { 1, 2 }));

    [Fact]
    public void NoRequestedClients_IsAlwaysValid()
    {
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden(null, new HashSet<int> { 1 }));
        Assert.Empty(UserClientAssignmentPolicy.FindForbidden([], new HashSet<int> { 1 }));
    }

    [Fact]
    public void DuplicatedRequestedIds_AreReportedOnce() =>
        Assert.Equal([9], UserClientAssignmentPolicy.FindForbidden([9, 9], new HashSet<int> { 1 }));

    [Fact]
    public void SA_ReplacesAllExistingAssignments() =>
        Assert.Equal([1, 2, 3], UserClientAssignmentPolicy.ClientIdsToReplace([1, 2, 3], allowed: null));

    [Fact]
    public void Admin_ReplacesOnlyTheAssignmentsHeSees_AndKeepsTheOnesAnotherPersonGave() =>
        Assert.Equal([1, 2], UserClientAssignmentPolicy.ClientIdsToReplace([1, 2, 77], new HashSet<int> { 1, 2, 3 }));
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~UserClientAssignmentPolicyTests" 2>&1 | grep -E " error " | sort -u | head -3`
Expected: error de compilación.

- [ ] **Step 3: Implementar la política**

`ZynstormECFPlatform.Services/Users/UserClientAssignmentPolicy.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Users;

/// <summary>
/// Reglas para asignar clientes a un usuario. <c>allowed</c> son los clientes que ve quien asigna;
/// null significa sin restricción (SA). Funciones puras.
/// </summary>
public static class UserClientAssignmentPolicy
{
    public const string ForbiddenMessage = "Alguno de los clientes seleccionados no pertenece a su cuenta.";

    /// <summary>Ids pedidos que quien asigna no puede ver.</summary>
    public static List<int> FindForbidden(IEnumerable<int>? requested, ISet<int>? allowed)
    {
        if (allowed is null || requested is null) return [];

        return requested.Distinct().Where(id => !allowed.Contains(id)).ToList();
    }

    /// <summary>
    /// Asignaciones existentes que un PUT reemplaza. SA reemplaza todas; un Admin solo las que él mismo ve,
    /// para no borrar las que otra persona le dio al usuario.
    /// </summary>
    public static List<int> ClientIdsToReplace(IEnumerable<int> existing, ISet<int>? allowed) =>
        allowed is null ? existing.ToList() : existing.Where(allowed.Contains).ToList();
}
```

- [ ] **Step 4: Correr y verificar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~UserClientAssignmentPolicyTests" 2>&1 | grep -E " error |Passed!|Failed" | sort -u`
Expected: `Passed!  - Failed: 0, Passed: 7`.

- [ ] **Step 5: Aplicarla en `UserController`**

En `ZynstormECFPlatform.Web.Api/Controllers/UserController.cs`:

1. Reemplazar:

```csharp
    private readonly ILogger<UserController> _logger = logger;
```

por:

```csharp
    private readonly ILogger<UserController> _logger = logger;

    private bool CallerIsSA
    {
        get
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role?.ToUpper() == "SA" || role == "1";
        }
    }

    /// <summary>Clientes que ve quien llama (sus UserClient). Null = SA, sin restricción.</summary>
    private async Task<HashSet<int>?> AllowedClientIdsAsync()
    {
        if (CallerIsSA) return null;

        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var links = await _userClientService.GetManyByAsync(x => x.UserId == callerId);

        return links.Select(x => x.ClientId).ToHashSet();
    }
```

2. En `Post`, justo después de `var creatorRole = User.FindFirst(ClaimTypes.Role)?.Value;`, agregar:

```csharp

            // Un Admin solo asigna clientes que él mismo ve (en un revendedor, los de su cuenta).
            var forbiddenClients = UserClientAssignmentPolicy.FindForbidden(dto.ClientIds, await AllowedClientIdsAsync());
            if (forbiddenClients.Count > 0)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = UserClientAssignmentPolicy.ForbiddenMessage, clientIds = forbiddenClients });
```

3. En `Put`, reemplazar:

```csharp
            var user = await _accountService.GetUserByIdAsync(dto.UserId);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName;
```

por:

```csharp
            // SA edita a cualquiera; un Admin solo a los usuarios que creó.
            var user = await GetManageableUserAsync(dto.UserId);
            if (user == null) return NotFound();

            var allowedClientIds = await AllowedClientIdsAsync();
            var forbiddenClients = UserClientAssignmentPolicy.FindForbidden(dto.ClientIds, allowedClientIds);
            if (forbiddenClients.Count > 0)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = UserClientAssignmentPolicy.ForbiddenMessage, clientIds = forbiddenClients });

            user.FirstName = dto.FirstName;
```

y reemplazar:

```csharp
            // Update Clients
            var existingClients = await _userClientService.GetManyByAsync(x => x.UserId == user.Id);
            foreach (var ec in existingClients)
            {
                await _userClientService.HardDeleteAsync(ec);
            }
```

por:

```csharp
            // Update Clients: un Admin solo reemplaza las asignaciones que él mismo ve.
            var existingClients = (await _userClientService.GetManyByAsync(x => x.UserId == user.Id)).ToList();
            var replaceable = UserClientAssignmentPolicy.ClientIdsToReplace(existingClients.Select(x => x.ClientId), allowedClientIds);
            foreach (var ec in existingClients.Where(x => replaceable.Contains(x.ClientId)))
            {
                await _userClientService.HardDeleteAsync(ec);
            }
```

4. En `Delete`, reemplazar:

```csharp
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;
```

por:

```csharp
            // SA borra a cualquiera; un Admin solo a los usuarios que creó.
            var user = await GetManageableUserAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;
```

5. Agregar `using ZynstormECFPlatform.Services.Users;` si falta (ya existe `using ZynstormECFPlatform.Services.Users;` en el archivo; verifica con `grep -n "Services.Users" ZynstormECFPlatform.Web.Api/Controllers/UserController.cs`).

- [ ] **Step 6: Compilar y correr**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Users" 2>&1 | grep -E "Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Failed: 0` (incluye `UserPageAccessManagerTests`).

- [ ] **Step 7: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services/Users ZynstormECFPlatform.Web.Api/Controllers/UserController.cs ZynstormECFPlatform.Tests/Users/UserClientAssignmentPolicyTests.cs && git commit -q -m "$(cat <<'EOF'
Un Admin solo asigna clientes que ve y solo edita o borra a sus usuarios

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 4: Servicio de vistas de cuenta, cupo y acceso del dueño

**Files:**
- Modify: `ZynstormECFPlatform.Services/Billing/BillingAccountPolicy.cs` (`ValidateQuota`)
- Modify: `ZynstormECFPlatform.Dtos/BillingAccountDtos.cs` (`ClientQuotaDto`)
- Create: `ZynstormECFPlatform.Services/Billing/IBillingAccountViewService.cs`, `BillingAccountViewService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`
- Rewrite: `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/BillingAccountPolicyTests.cs`

**Interfaces:**
- Produces:
  - `BillingAccountPolicy.ValidateQuota(int? quota)` → `List<string>`
  - `ClientQuotaDto { int? Quota }`
  - `IBillingAccountViewService`: `GetAllAsync(ct)` → `List<BillingAccountViewDto>`; `GetDetailAsync(string guid, ct)` → `BillingAccountDetailDto?`; `FindGuidByOwnerAsync(string ownerUserId, ct)` → `string?`; `SetClientQuotaAsync(string accountGuid, string clientGuid, int? quota, ct)` → `PaymentRegistrationResult<bool>`.
- Cambia: `SetClients` del controlador del SA además da al dueño el `UserClient` de los clientes asignados. Nuevo endpoint SA: `PUT v1/BillingAccount/guid/{guid}/clients/{clientGuid}/quota`.

- [ ] **Step 1: Pruebas de `ValidateQuota` (fallan: no existe)**

En `ZynstormECFPlatform.Tests/Billing/BillingAccountPolicyTests.cs`, reemplazar:

```csharp
    [Theory]
    [InlineData(100, null, false)]
```

por:

```csharp
    [Fact]
    public void Quota_CanBeClearedOrZeroOrPositive()
    {
        Assert.Empty(BillingAccountPolicy.ValidateQuota(null));
        Assert.Empty(BillingAccountPolicy.ValidateQuota(0));
        Assert.Empty(BillingAccountPolicy.ValidateQuota(500));
    }

    [Fact]
    public void Quota_CannotBeNegative() =>
        Assert.Contains(BillingAccountPolicy.ValidateQuota(-1), e => e.Contains("negativo"));

    [Theory]
    [InlineData(100, null, false)]
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountPolicyTests" 2>&1 | grep -E " error " | sort -u | head -3`
Expected: error de compilación (`ValidateQuota`).

- [ ] **Step 3: `ValidateQuota` y el DTO**

En `BillingAccountPolicy.cs`, reemplazar:

```csharp
    /// <summary>El cupo es informativo: solo sirve para mostrar una alerta, nunca bloquea la emisión.</summary>
```

por:

```csharp
    public static List<string> ValidateQuota(int? quota) =>
        quota < 0 ? ["El cupo no puede ser negativo."] : [];

    /// <summary>El cupo es informativo: solo sirve para mostrar una alerta, nunca bloquea la emisión.</summary>
```

En `ZynstormECFPlatform.Dtos/BillingAccountDtos.cs`, reemplazar:

```csharp
public class BillingAccountClientUsageDto
```

por:

```csharp
/// <summary>Cupo mensual informativo de un cliente de la cuenta. Null lo quita.</summary>
public class ClientQuotaDto
{
    public int? Quota { get; set; }
}

public class BillingAccountClientUsageDto
```

- [ ] **Step 4: Crear el servicio de vistas**

`ZynstormECFPlatform.Services/Billing/IBillingAccountViewService.cs`:

```csharp
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public interface IBillingAccountViewService
{
    Task<List<BillingAccountViewDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BillingAccountDetailDto?> GetDetailAsync(string accountGuid, CancellationToken cancellationToken = default);

    /// <summary>GUID de la cuenta de la que el usuario es dueño; null si no tiene.</summary>
    Task<string?> FindGuidByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    /// <summary>Fija o quita el cupo informativo de un cliente que pertenece a la cuenta.</summary>
    Task<PaymentRegistrationResult<bool>> SetClientQuotaAsync(
        string accountGuid, string clientGuid, int? quota, CancellationToken cancellationToken = default);
}
```

`ZynstormECFPlatform.Services/Billing/BillingAccountViewService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public class BillingAccountViewService(
    IBillingAccountService accountService,
    IBillingAccountPaymentService paymentService,
    IBillingAccountMonthlyUsageService usageService,
    IClientMonthlyUsageService clientUsageService,
    IClientService clientService,
    IPlanOverageTierService tierService) : IBillingAccountViewService
{
    private IQueryable<BillingAccount> AccountsWithDetails() =>
        accountService.Table.AsNoTracking().Include(a => a.Plan).Include(a => a.OwnerUser);

    public async Task<List<BillingAccountViewDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await AccountsWithDetails().OrderBy(a => a.Name).ToListAsync(cancellationToken);
        return await BuildViewsAsync(accounts, cancellationToken);
    }

    public async Task<string?> FindGuidByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
        await accountService.Table.AsNoTracking()
            .Where(a => a.OwnerUserId == ownerUserId)
            .Select(a => a.GuidId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<BillingAccountDetailDto?> GetDetailAsync(string accountGuid, CancellationToken cancellationToken = default)
    {
        var account = await AccountsWithDetails().FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);
        if (account == null) return null;

        var view = (await BuildViewsAsync([account], cancellationToken)).Single();
        var today = DateTimeExtensions.DrNow;

        var clients = await clientService.Table.AsNoTracking()
            .Where(c => c.BillingAccountId == account.BillingAccountId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var clientIds = clients.Select(c => c.ClientId).ToList();
        var accepted = await clientUsageService.Table.AsNoTracking()
            .Where(u => clientIds.Contains(u.ClientId) && u.Year == today.Year && u.Month == today.Month)
            .ToDictionaryAsync(u => u.ClientId, u => u.AcceptedDocuments, cancellationToken);

        var detail = new BillingAccountDetailDto();
        CopyView(view, detail);
        detail.Clients = clients.Select(c =>
        {
            var documents = accepted.GetValueOrDefault(c.ClientId);
            return new BillingAccountClientUsageDto
            {
                ClientGuidId = c.GuidId,
                ClientName = c.Name,
                ClientRnc = c.Rnc,
                AcceptedDocuments = documents,
                QuotaDocuments = c.QuotaDocuments,
                QuotaExceeded = BillingAccountPolicy.IsQuotaExceeded(documents, c.QuotaDocuments)
            };
        }).ToList();

        return detail;
    }

    public async Task<PaymentRegistrationResult<bool>> SetClientQuotaAsync(
        string accountGuid, string clientGuid, int? quota, CancellationToken cancellationToken = default)
    {
        var errors = BillingAccountPolicy.ValidateQuota(quota);
        if (errors.Count > 0) return PaymentRegistrationResult<bool>.Invalid(errors);

        var account = await accountService.Table.AsNoTracking()
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);
        if (account == null) return PaymentRegistrationResult<bool>.NotFound("Cuenta no encontrada.");

        var client = await clientService.Table
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid && c.BillingAccountId == account.BillingAccountId, cancellationToken);
        if (client == null) return PaymentRegistrationResult<bool>.NotFound("El cliente no pertenece a la cuenta.");

        client.QuotaDocuments = quota;
        await clientService.UpdateAsync(client);

        return PaymentRegistrationResult<bool>.Success(true);
    }

    private async Task<List<BillingAccountViewDto>> BuildViewsAsync(IReadOnlyList<BillingAccount> accounts, CancellationToken cancellationToken)
    {
        if (accounts.Count == 0) return [];

        var today = DateTimeExtensions.DrNow;
        var ids = accounts.Select(a => a.BillingAccountId).ToList();
        var planIds = accounts.Select(a => a.PlanId).Distinct().ToList();

        var poolNow = await usageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.BillingAccountId) && u.Year == today.Year && u.Month == today.Month)
            .ToDictionaryAsync(u => u.BillingAccountId, u => u.AcceptedDocuments, cancellationToken);

        var tiers = await tierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        var clientCounts = await clientService.Table.AsNoTracking()
            .Where(c => c.BillingAccountId != null && ids.Contains(c.BillingAccountId.Value))
            .GroupBy(c => c.BillingAccountId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var pending = await paymentService.GetPendingOveragesAsync(ids, cancellationToken);

        return accounts.Select(a =>
        {
            var terms = BillingTerms.For(a);
            var documents = poolNow.GetValueOrDefault(a.BillingAccountId);
            var calculation = BillingCalculator.Calculate(
                PaymentCalculator.GetMonthlyFee(terms),
                a.Plan.MonthlyDocumentLimit,
                tiers.Where(t => t.PlanId == a.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                documents);
            var (status, days) = PaymentCalculator.GetPaymentStatus(a.NextPaymentDate, PaymentCalculator.DefaultWarningDays);
            var accountPending = pending.GetValueOrDefault(a.BillingAccountId);

            return new BillingAccountViewDto
            {
                GuidId = a.GuidId,
                Name = a.Name,
                OwnerUserId = a.OwnerUserId,
                OwnerName = a.OwnerUser?.FullName ?? string.Empty,
                OwnerEmail = a.OwnerUser?.Email,
                PlanId = a.PlanId,
                PlanName = a.Plan.Name,
                MonthlyDocumentLimit = a.Plan.MonthlyDocumentLimit,
                MonthlyFee = PaymentCalculator.GetMonthlyFee(terms),
                CustomMonthlyFee = a.CustomMonthlyFee,
                StatusId = a.StatusId,
                ClientsCount = clientCounts.GetValueOrDefault(a.BillingAccountId),
                CurrentMonthAcceptedDocuments = documents,
                CurrentMonthOverageDocuments = calculation.OverageDocuments,
                CurrentMonthOverageAmount = calculation.OverageAmount,
                PaidMonths = a.PaidMonths,
                PrepaymentDiscountPercent = a.PrepaymentDiscountPercent,
                PaymentGraceDays = PaymentReminderPolicy.NormalizeGraceDays(a.PaymentGraceDays),
                LastPaymentDate = a.LastPaymentDate,
                NextPaymentDate = a.NextPaymentDate,
                PaymentStatus = (int)status,
                PaymentDaysToDue = days,
                PaymentSuspended = a.PaymentSuspendedAtUtc != null,
                PendingOverageAmount = accountPending?.Sum(p => p.Amount) ?? 0m,
                PendingOverageMonths = accountPending?.Count ?? 0
            };
        }).ToList();
    }

    private static void CopyView(BillingAccountViewDto from, BillingAccountViewDto to)
    {
        foreach (var property in typeof(BillingAccountViewDto).GetProperties().Where(p => p.CanRead && p.CanWrite))
            property.SetValue(to, property.GetValue(from));
    }
}
```

En `ServiceCollectionExtensions.cs`, reemplazar:

```csharp
        services.AddTransient<Billing.IBillingAccountReminderService, Billing.BillingAccountReminderService>();
```

por:

```csharp
        services.AddTransient<Billing.IBillingAccountReminderService, Billing.BillingAccountReminderService>();
        services.AddTransient<Billing.IBillingAccountViewService, Billing.BillingAccountViewService>();
```

- [ ] **Step 5: Reescribir `BillingAccountController`**

Reemplazar el contenido completo de `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs`:

```csharp
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>Cuentas de facturación: un plan de comprobantes compartido por los clientes de un revendedor.</summary>
[Authorize(Roles = "SA")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class BillingAccountController(
    IBillingAccountService accountService,
    IBillingAccountPaymentService paymentService,
    IBillingAccountViewService viewService,
    IClientService clientService,
    IUserClientService userClientService,
    IPlanService planService,
    IAccountService userAccountService,
    IUnitOfWork unitOfWork,
    ILogger<BillingAccountController> logger) : ControllerBase
{
    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await viewService.GetAllAsync(cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error listando las cuentas de facturación");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}")]
    public async Task<IActionResult> GetByGuid(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            var detail = await viewService.GetDetailAsync(guid, cancellationToken);
            return detail == null ? NotFound(new { message = "Cuenta no encontrada." }) : Ok(detail);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] BillingAccountSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await planService.GetNoTrackingByAsync(p => p.PlanId == dto.PlanId, cancellationToken);
            var errors = BillingAccountPolicy.ValidateAccount(
                dto.Name, plan, dto.CustomMonthlyFee, dto.PaidMonths, dto.PrepaymentDiscountPercent, dto.PaymentGraceDays);

            var owner = string.IsNullOrWhiteSpace(dto.OwnerUserId) ? null : await userAccountService.GetUserByIdAsync(dto.OwnerUserId);
            var ownerIsAdmin = owner != null && await userAccountService.IsUserInRoleAsync(owner, "Admin");
            var ownerHasAccount = owner != null && await accountService.Table.AnyAsync(a => a.OwnerUserId == owner.Id, cancellationToken);
            errors.AddRange(BillingAccountPolicy.ValidateOwner(owner != null, ownerIsAdmin, ownerHasAccount));

            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            var account = new BillingAccount
            {
                Name = dto.Name.Trim(),
                OwnerUserId = owner!.Id,
                PlanId = plan!.PlanId,
                StatusId = (int)StatusEnum.Active,
                CustomMonthlyFee = dto.CustomMonthlyFee,
                OfferStartDate = OfferPricing.ResolveStartDate(
                    null, plan.PlanId, OfferPricing.PlanHasOffer(plan.OfferPrice, plan.OfferMonths), null, DateTimeExtensions.DrNow.Date),
                PaidMonths = dto.PaidMonths,
                PrepaymentDiscountPercent = dto.PrepaymentDiscountPercent,
                PaymentGraceDays = dto.PaymentGraceDays,
                LastPaymentDate = dto.LastPaymentDate?.Date,
                NextPaymentDate = dto.NextPaymentDate?.Date
            };

            var inserted = await accountService.InsertAsync(account);
            return Ok(await viewService.GetDetailAsync(inserted!.GuidId, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Error creando la cuenta de facturación");
            return Conflict(new { message = "El usuario ya tiene una cuenta de facturación." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creando la cuenta de facturación");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("guid/{guid}")]
    public async Task<IActionResult> Update(string guid, [FromBody] BillingAccountSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await accountService.Table.Include(a => a.Plan).FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
            if (account == null) return NotFound(new { message = "Cuenta no encontrada." });

            var plan = await planService.GetNoTrackingByAsync(p => p.PlanId == dto.PlanId, cancellationToken);
            var errors = BillingAccountPolicy.ValidateAccount(
                dto.Name, plan, dto.CustomMonthlyFee, dto.PaidMonths, dto.PrepaymentDiscountPercent, dto.PaymentGraceDays);
            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            var previousPlanId = account.PlanId;
            account.Name = dto.Name.Trim();
            account.PlanId = plan!.PlanId;
            account.CustomMonthlyFee = dto.CustomMonthlyFee;
            account.OfferStartDate = OfferPricing.ResolveStartDate(
                previousPlanId, plan.PlanId, OfferPricing.PlanHasOffer(plan.OfferPrice, plan.OfferMonths),
                account.OfferStartDate, DateTimeExtensions.DrNow.Date);
            account.PaidMonths = dto.PaidMonths;
            account.PrepaymentDiscountPercent = dto.PrepaymentDiscountPercent;
            account.PaymentGraceDays = dto.PaymentGraceDays;
            account.LastPaymentDate = dto.LastPaymentDate?.Date;
            account.NextPaymentDate = dto.NextPaymentDate?.Date;

            // Como en un cliente: poner una fecha de pago futura reactiva la cuenta suspendida.
            if (account.PaymentSuspendedAtUtc != null && account.NextPaymentDate > DateTimeExtensions.DrNow.Date)
                account.PaymentSuspendedAtUtc = null;

            await accountService.UpdateAsync(account);
            return Ok(await viewService.GetDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error actualizando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Deja como miembros exactamente los clientes indicados: asigna los nuevos y suelta a los que ya no figuran.
    /// El PlanId propio de un cliente asignado se conserva pero deja de cobrarse. Al asignar, el dueño recibe
    /// el acceso (UserClient) al cliente; al soltar, el acceso del dueño no se quita.
    /// </summary>
    [HttpPut("guid/{guid}/clients")]
    public async Task<IActionResult> SetClients(string guid, [FromBody] BillingAccountClientsDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await accountService.Table.FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
            if (account == null) return NotFound(new { message = "Cuenta no encontrada." });

            var requested = dto.ClientGuidIds.Distinct().ToList();
            var affected = await clientService.Table
                .Where(c => requested.Contains(c.GuidId) || c.BillingAccountId == account.BillingAccountId)
                .ToListAsync(cancellationToken);

            var requestedClients = affected.Where(c => requested.Contains(c.GuidId)).ToList();
            var errors = BillingAccountPolicy.ValidateMembers(requestedClients, requested.Count, account.BillingAccountId);
            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            foreach (var client in affected)
                client.BillingAccountId = requested.Contains(client.GuidId) ? account.BillingAccountId : null;

            // El dueño debe ver a sus clientes en listados, documentos y dashboard: todo eso se rige por UserClient.
            var requestedIds = requestedClients.Select(c => c.ClientId).ToList();
            var ownerLinks = (await userClientService.GetManyByAsync(
                    x => x.UserId == account.OwnerUserId && requestedIds.Contains(x.ClientId), cancellationToken))
                .Select(x => x.ClientId)
                .ToHashSet();

            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await clientService.UpdateAsync(affected);

                foreach (var clientId in requestedIds.Where(id => !ownerLinks.Contains(id)))
                    await userClientService.InsertAsync(new UserClient { UserId = account.OwnerUserId, ClientId = clientId });
            }, cancellationToken);

            return Ok(await viewService.GetDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error asignando clientes a la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("guid/{guid}/clients/{clientGuid}/quota")]
    public async Task<IActionResult> SetClientQuota(string guid, string clientGuid, [FromBody] ClientQuotaDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await viewService.SetClientQuotaAsync(guid, clientGuid, dto.Quota, cancellationToken);
            if (result.Outcome != PaymentRegistrationOutcome.Ok) return ToActionResult(result);

            return Ok(await viewService.GetDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error fijando el cupo del cliente {ClientGuid} en la cuenta {Guid}", clientGuid, guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}/payments/preview")]
    public async Task<IActionResult> GetPaymentPreview(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.GetPreviewAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el pago a registrar de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el pago a registrar." });
        }
    }

    [HttpPost("guid/{guid}/payments")]
    public async Task<IActionResult> RegisterPayment(string guid, [FromBody] RegisterPaymentRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.RegisterAsync(guid, dto, CurrentUserId, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error registrando el pago de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo registrar el pago." });
        }
    }

    [HttpGet("guid/{guid}/payments")]
    public async Task<IActionResult> GetPaymentHistory(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.GetHistoryAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el historial de pagos de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el historial de pagos." });
        }
    }

    private IActionResult ToActionResult<T>(PaymentRegistrationResult<T> result) => result.Outcome switch
    {
        PaymentRegistrationOutcome.Ok => Ok(result.Value),
        PaymentRegistrationOutcome.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
        PaymentRegistrationOutcome.Conflict => Conflict(new { message = string.Join(" ", result.Errors), errors = result.Errors }),
        _ => BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors })
    };
}
```

- [ ] **Step 6: Compilar y correr**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -8` y `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountPolicyTests" 2>&1 | grep -E "Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Passed!  - Failed: 0, Passed: 22`.

- [ ] **Step 7: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services ZynstormECFPlatform.Dtos ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs ZynstormECFPlatform.Tests/Billing/BillingAccountPolicyTests.cs && git commit -q -m "$(cat <<'EOF'
Extraer las vistas de cuenta a un servicio, fijar el cupo y dar acceso al dueño

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 5: `MyBillingAccountController` (el Admin dueño)

**Files:**
- Create: `ZynstormECFPlatform.Web.Api/Controllers/MyBillingAccountController.cs`

**Interfaces:**
- Consumes: `IBillingAccountViewService`, `IBillingAccountPaymentService` (Task 4 y fase 2).
- Produces (rutas bajo `v1/MyBillingAccount`, rol Admin; 404 si el Admin no tiene cuenta):
  - `GET ""` → `BillingAccountDetailDto` (pool, consumo y cupo por cliente, excedente proyectado)
  - `GET "payments"` → `List<PaymentReceiptDto>` (solo lectura; no hay endpoints para registrar pagos)
  - `PUT "clients/{clientGuid}/quota"` (`ClientQuotaDto`) → `BillingAccountDetailDto`

Solo se opera sobre la cuenta cuyo `OwnerUserId` es el usuario autenticado: el GUID nunca viene del cliente HTTP, así un Admin no puede apuntar a la cuenta de otro.

- [ ] **Step 1: Escribir el controlador**

`ZynstormECFPlatform.Web.Api/Controllers/MyBillingAccountController.cs`:

```csharp
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>
/// La cuenta de facturación del Admin que la dirige: consulta del pool y de los pagos (solo lectura) y cupo
/// informativo por cliente. La cuenta se resuelve siempre por el usuario autenticado.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class MyBillingAccountController(
    IBillingAccountViewService viewService,
    IBillingAccountPaymentService paymentService,
    ILogger<MyBillingAccountController> logger) : ControllerBase
{
    private const string NoAccount = "No tiene una cuenta de facturación.";

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private async Task<string?> OwnAccountGuidAsync(CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(CurrentUserId) ? null : await viewService.FindGuidByOwnerAsync(CurrentUserId, cancellationToken);

    [HttpGet("")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            var guid = await OwnAccountGuidAsync(cancellationToken);
            if (guid == null) return NotFound(new { message = NoAccount });

            var detail = await viewService.GetDetailAsync(guid, cancellationToken);
            return detail == null ? NotFound(new { message = NoAccount }) : Ok(detail);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando la cuenta de facturación propia");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken = default)
    {
        try
        {
            var guid = await OwnAccountGuidAsync(cancellationToken);
            if (guid == null) return NotFound(new { message = NoAccount });

            var result = await paymentService.GetHistoryAsync(guid, cancellationToken);
            return result.Outcome == PaymentRegistrationOutcome.Ok
                ? Ok(result.Value)
                : NotFound(new { message = result.Errors.FirstOrDefault() ?? NoAccount });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el historial de pagos de la cuenta propia");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("clients/{clientGuid}/quota")]
    public async Task<IActionResult> SetClientQuota(string clientGuid, [FromBody] ClientQuotaDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var guid = await OwnAccountGuidAsync(cancellationToken);
            if (guid == null) return NotFound(new { message = NoAccount });

            var result = await viewService.SetClientQuotaAsync(guid, clientGuid, dto.Quota, cancellationToken);

            return result.Outcome switch
            {
                PaymentRegistrationOutcome.Ok => Ok(await viewService.GetDetailAsync(guid, cancellationToken)),
                PaymentRegistrationOutcome.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
                _ => BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors })
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error fijando el cupo del cliente {ClientGuid} en la cuenta propia", clientGuid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
```

- [ ] **Step 2: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Web.Api/Controllers/MyBillingAccountController.cs && git commit -q -m "$(cat <<'EOF'
Endpoints de solo lectura de la cuenta propia para el Admin dueño

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 6: Aislamiento — `ClientCertificateController`

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientCertificateController.cs`

**Interfaces:**
- Consumes: `IClientAccessService` (Task 1).
- Cambia: ningún endpoint nuevo. Un Admin solo lee, sube, borra y relee certificados de clientes que ve; el listado sin filtro devuelve solo los suyos. `Put` queda solo para SA (el DTO es el de la subida de archivo y no se usa por JSON). SA no cambia.

Esta clase depende de EF y del hosting; no tiene prueba unitaria. La regla de acceso ya está probada en Task 1.

- [ ] **Step 1: Inyectar la guía**

En `ClientCertificateController.cs`, reemplazar:

```csharp
        IEncryptedService encryptedService,
        IUnitOfWork unitOfWork,
```

por:

```csharp
        IEncryptedService encryptedService,
        IClientAccessService clientAccess,
        IUnitOfWork unitOfWork,
```

y agregar `using ZynstormECFPlatform.Services.Users;` junto a los demás `using` (después de `using ZynstormECFPlatform.Services.Certificates;`).

- [ ] **Step 2: `Get` — por id, por cliente y sin filtro**

Reemplazar:

```csharp
                    var result = await certificateService.GetByAsync(x => x.GuidId == id, cancellationToken);

                    if (result == null) return NotFound();

                    return Ok(Mapper.Map<ClientCertificate, ClientCertificateViewDto>(result));
```

por:

```csharp
                    var result = await certificateService.GetByAsync(x => x.GuidId == id, cancellationToken);

                    if (result == null || !await clientAccess.CanAccessClientAsync(User, result.ClientId, cancellationToken))
                        return NotFound();

                    return Ok(Mapper.Map<ClientCertificate, ClientCertificateViewDto>(result));
```

Reemplazar:

```csharp
                    if (client == null)
                        return NotFound("No se encontró un cliente con el GuidId proporcionado.");

                    // Usamos el ClientId interno para filtrar los certificados
```

por:

```csharp
                    if (client == null || !await clientAccess.CanAccessClientAsync(User, client.ClientId, cancellationToken))
                        return NotFound("No se encontró un cliente con el GuidId proporcionado.");

                    // Usamos el ClientId interno para filtrar los certificados
```

Reemplazar:

```csharp
                else
                {
                    results = await certificateService.GetAllAsync(cancellationToken);
                }

                return Ok(Mapper.Map<IEnumerable<ClientCertificate>, IEnumerable<ClientCertificateViewDto>>(results));
```

por:

```csharp
                else
                {
                    // SA ve todos; el resto, solo los de sus clientes.
                    var accessibleIds = await clientAccess.GetAccessibleClientIdsAsync(User, cancellationToken);
                    results = accessibleIds == null
                        ? await certificateService.GetAllAsync(cancellationToken)
                        : await certificateService.GetManyByAsync(x => accessibleIds.Contains(x.ClientId), cancellationToken);
                }

                return Ok(Mapper.Map<IEnumerable<ClientCertificate>, IEnumerable<ClientCertificateViewDto>>(results));
```

- [ ] **Step 3: `Post` y `RefreshDetails`**

En `Post`, reemplazar:

```csharp
                if (client == null)
                    return NotFound($"No se encontró un cliente con el GuidId proporcionado.");
```

por:

```csharp
                if (client == null || !await clientAccess.CanAccessClientAsync(User, client.ClientId))
                    return NotFound($"No se encontró un cliente con el GuidId proporcionado.");
```

En `RefreshDetails`, reemplazar:

```csharp
                    if (client == null)
                        return NotFound("No se encontró un cliente con el GuidId proporcionado.");

                    certificates = await certificateService.GetManyByAsync(x => x.ClientId == client.ClientId, cancellationToken);
                }
                else
                {
                    certificates = await certificateService.GetAllAsync(cancellationToken);
                }
```

por:

```csharp
                    if (client == null || !await clientAccess.CanAccessClientAsync(User, client.ClientId, cancellationToken))
                        return NotFound("No se encontró un cliente con el GuidId proporcionado.");

                    certificates = await certificateService.GetManyByAsync(x => x.ClientId == client.ClientId, cancellationToken);
                }
                else
                {
                    // Sin cliente: SA relee todos; el resto, solo los de sus clientes.
                    var accessibleIds = await clientAccess.GetAccessibleClientIdsAsync(User, cancellationToken);
                    certificates = accessibleIds == null
                        ? await certificateService.GetAllAsync(cancellationToken)
                        : await certificateService.GetManyByAsync(x => accessibleIds.Contains(x.ClientId), cancellationToken);
                }
```

- [ ] **Step 4: Endpoints heredados de `BaseController`**

Insertar estos métodos justo antes del comentario `/// <summary>` de `RefreshDetails` (reemplazar `        /// <summary>\n        /// Relee los certificados ya registrados para completar los datos del titular y del emisor.` por el bloque siguiente seguido de esas dos líneas):

```csharp
        // Los endpoints heredados de BaseController no conocen al cliente dueño del certificado.

        [HttpGet]
        [Route("guid/{guid}", Order = 1)]
        public override async Task<ActionResult<ClientCertificateViewDto>> GetByGuid(string guid, CancellationToken cancellationToken = default)
        {
            var certificate = await certificateService.GetByAsync(x => x.GuidId == guid, cancellationToken);
            if (certificate == null || !await clientAccess.CanAccessClientAsync(User, certificate.ClientId, cancellationToken))
                return NotFound();

            return await base.GetByGuid(guid, cancellationToken);
        }

        [HttpDelete]
        [Route("", Order = 1)]
        public override async Task<IActionResult> Delete([FromQuery] string id)
        {
            var certificate = await certificateService.GetByAsync(x => x.GuidId == id);
            if (certificate == null || !await clientAccess.CanAccessClientAsync(User, certificate.ClientId))
                return NotFound();

            return await base.Delete(id);
        }

        [HttpDelete]
        [Route("guid/{guid}", Order = 1)]
        public override async Task<IActionResult> DeleteByGuid(string guid)
        {
            var certificate = await certificateService.GetByAsync(x => x.GuidId == guid);
            if (certificate == null || !await clientAccess.CanAccessClientAsync(User, certificate.ClientId))
                return NotFound();

            return await base.DeleteByGuid(guid);
        }

        [HttpPut]
        [Route("", Order = 1)]
        [Authorize(Roles = "SA")]
        public override Task<ActionResult<ClientCertificateViewDto>> Put([FromBody] ClientCertificateCreateDto dto) => base.Put(dto);

```

- [ ] **Step 5: Compilar y correr toda la suite**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -8` y `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed Z" | sort -u`
Expected: `Build succeeded.` y solo la falla ya conocida `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada`.

Si el compilador rechaza algún `override` (por ejemplo porque `Delete` o `Put` no son `virtual` con esa firma), compara con `BaseController.cs:152-262` y ajusta la firma; los atributos `[Http*]`/`[Route]` se repiten a propósito.

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Web.Api/Controllers/ClientCertificateController.cs && git commit -q -m "$(cat <<'EOF'
Un Admin solo ve y administra certificados de sus clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 7: Aislamiento — `RiFormatController`

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/RiFormatController.cs`

**Interfaces:**
- Consumes: `IClientAccessService` (Task 1).
- Cambia: los endpoints por cliente (`assignment`, `resolved`, `logo` GET/PUT) responden 404 si el usuario no ve al cliente. La biblioteca global (`client-types` y `templates`: crear, editar, borrar, asociar) pasa a **solo SA**: hoy cualquier usuario autenticado puede borrar formatos que usan todos los clientes. Las lecturas de la biblioteca no cambian.

**Cambio de comportamiento a confirmar con el usuario:** si hay un Admin de Zynstorm que hoy sube formatos desde la pantalla de certificación, dejará de poder hacerlo. Es un solo atributo por acción y se revierte quitándolo.

- [ ] **Step 1: Inyectar la guía**

Reemplazar:

```csharp
public class RiFormatController(IRiFormatService formatService) : ControllerBase
```

por:

```csharp
public class RiFormatController(IRiFormatService formatService, IClientAccessService clientAccess) : ControllerBase
```

y agregar `using ZynstormECFPlatform.Services.Users;` después de `using ZynstormECFPlatform.Dtos;`.

- [ ] **Step 2: Biblioteca global solo SA**

Reemplazar cada una de estas líneas por la misma con el atributo antes:

```csharp
    [HttpPost("client-types")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpPost("client-types")]
```

```csharp
    [HttpPut("client-types/{guidId}")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpPut("client-types/{guidId}")]
```

```csharp
    [HttpDelete("client-types/{guidId}")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpDelete("client-types/{guidId}")]
```

```csharp
    [HttpPost("client-types/{guidId}/templates/{templateGuidId}")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpPost("client-types/{guidId}/templates/{templateGuidId}")]
```

```csharp
    [HttpDelete("client-types/{guidId}/templates/{templateGuidId}")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpDelete("client-types/{guidId}/templates/{templateGuidId}")]
```

```csharp
    [HttpPost("templates")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpPost("templates")]
```

```csharp
    [HttpDelete("templates/{guidId}")]
```
→
```csharp
    [Authorize(Roles = "SA")]
    [HttpDelete("templates/{guidId}")]
```

- [ ] **Step 3: Endpoints por cliente con la guía**

Reemplazar:

```csharp
        Run(async () =>
        {
            await formatService.AssignToClientAsync(clientGuidId, dto);
            return NoContent();
        });
```

por:

```csharp
        Run(async () =>
        {
            if (!await clientAccess.CanAccessClientAsync(User, clientGuidId)) return NotFound();
            await formatService.AssignToClientAsync(clientGuidId, dto);
            return NoContent();
        });
```

Reemplazar:

```csharp
        Run(async () => Ok(await formatService.GetResolvedAsync(clientGuidId)));
```

por:

```csharp
        Run(async () =>
            await clientAccess.CanAccessClientAsync(User, clientGuidId)
                ? Ok(await formatService.GetResolvedAsync(clientGuidId))
                : NotFound());
```

Reemplazar:

```csharp
        Run(async () => Ok(await formatService.GetClientLogoAsync(clientGuidId)));
```

por:

```csharp
        Run(async () =>
            await clientAccess.CanAccessClientAsync(User, clientGuidId)
                ? Ok(await formatService.GetClientLogoAsync(clientGuidId))
                : NotFound());
```

Reemplazar:

```csharp
        Run(async () =>
        {
            await formatService.SetClientLogoAsync(clientGuidId, dto);
            return NoContent();
        });
```

por:

```csharp
        Run(async () =>
        {
            if (!await clientAccess.CanAccessClientAsync(User, clientGuidId)) return NotFound();
            await formatService.SetClientLogoAsync(clientGuidId, dto);
            return NoContent();
        });
```

Si el compilador no infiere el tipo de retorno de los lambdas con `?:` (un `OkObjectResult` y un `NotFoundResult` no tienen tipo común), conviértelos a bloque con `if`/`return` como los otros dos, que sí compilan porque el delegado es `Func<Task<ActionResult>>`.

- [ ] **Step 4: Compilar y correr toda la suite**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -8` y `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed Z" | sort -u`
Expected: `Build succeeded.` y solo la falla ya conocida de Certificación.

- [ ] **Step 5: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Web.Api/Controllers/RiFormatController.cs && git commit -q -m "$(cat <<'EOF'
Formatos RI: acceso por cliente y biblioteca global solo para SA

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 8: Verificación y hallazgos pendientes

**Files:** ninguno.

- [ ] **Step 1: Compilar todo y correr la suite completa**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u` y `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed Z" | sort -u`
Expected: `Build succeeded.` y la única falla es `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada` (ya fallaba antes de la fase 1).

- [ ] **Step 2: Comprobar que ningún endpoint de cuenta quedó abierto**

Run: `grep -n "Authorize" ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs ZynstormECFPlatform.Web.Api/Controllers/MyBillingAccountController.cs`
Expected: `BillingAccountController` con `[Authorize(Roles = "SA")]` y `MyBillingAccountController` con `[Authorize(Roles = "Admin")]`, ambos a nivel de clase.

- [ ] **Step 3: Reportar al usuario, incluyendo lo que NO se cerró**

Informar el resultado de la suite y que no se tocó ninguna base. Dejar claros estos hallazgos que quedan fuera de esta fase y que importan antes de darle rol Admin a un revendedor real:

1. **`CertificationController` no tiene `[Authorize]`** (ni hay política global): todos sus endpoints (simulaciones, descargas de XML por `jobId`, `ri/{clientGuidId}/...`) responden sin autenticación. Cerrarlo exige confirmar que la pantalla de certificación y las herramientas que lo usan envían token; es una decisión aparte.
2. **`FeController`** (anónimo por diseño, es el receptor de DGII) expone `GET client/{clientId}/received-files*` y `POST .../send-email` por `clientId` entero sin autenticación ni pertenencia.
3. **`DgiiController.download-rnc`** sigue abierto a todo Admin (descarga global del padrón; no expone datos de clientes).
4. **Asignaciones previas:** los clientes que el SA asignó a una cuenta con la fase 2 (antes de este cambio) no tienen `UserClient` del dueño; hay que volver a llamar `PUT BillingAccount/guid/{guid}/clients` con la misma lista para que se creen.
5. **Soltar un cliente de la cuenta** no quita el `UserClient` del dueño.
6. **Confirmar con el usuario** el cambio de `RiFormatController` (biblioteca solo SA), por si un Admin de Zynstorm sube formatos hoy.

Recordar lo que sigue: la fase 4 (pantallas `/cuenta`, `consumo`, `pagos`, formulario de cliente) y la verificación contra PostgreSQL pendiente de las fases 1 y 2.
