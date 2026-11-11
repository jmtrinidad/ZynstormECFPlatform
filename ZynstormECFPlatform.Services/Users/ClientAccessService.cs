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
