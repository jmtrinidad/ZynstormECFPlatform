using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common.Enums;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Users;

public interface IUserPageAccessManager
{
    /// <summary>
    /// Páginas asignadas al usuario. Null si nunca se le guardaron permisos (ve todo lo de su
    /// rol) o si su rol no es restringido.
    /// </summary>
    Task<List<string>?> GetAsync(User user, string? roleName);

    /// <summary>
    /// Si el usuario tiene el permiso indicado (una página o una sección como
    /// <see cref="UserPageAccessManager.DashboardRecentActivity"/>). Sin permisos guardados ve todo.
    /// </summary>
    Task<bool> HasAccessAsync(User user, string? roleName, string key);

    /// <summary>Reemplaza las páginas del usuario y su página de inicio.</summary>
    Task SaveAsync(User user, string? roleName, IEnumerable<string>? pageAccess, string? homePagePath);
}

public class UserPageAccessManager(
    IUserPageAccessService pageAccessRepository,
    IAccountService accountService) : IUserPageAccessManager
{
    private readonly IUserPageAccessService _pageAccessRepository = pageAccessRepository;
    private readonly IAccountService _accountService = accountService;

    // Sección del Dashboard que se asigna como un permiso más. Igual que en el frontend
    // (constants/page-access.ts).
    public const string DashboardRecentActivity = "dashboard:recent-activity";

    // SA y Admin acceden a todo, por lo que no se les persiste restricción.
    public static bool IsPageRestricted(string? roleName) =>
        !string.Equals(roleName, nameof(UserType.SA), StringComparison.OrdinalIgnoreCase)
        && !string.Equals(roleName, nameof(UserType.Admin), StringComparison.OrdinalIgnoreCase);

    public async Task<List<string>?> GetAsync(User user, string? roleName)
    {
        if (!IsPageRestricted(roleName) || !user.PageAccessConfigured)
            return null;

        var rows = await _pageAccessRepository.GetManyByAsync(p => p.UserId == user.Id);
        return rows.Select(p => p.PagePath).ToList();
    }

    public async Task<bool> HasAccessAsync(User user, string? roleName, string key)
    {
        var granted = await GetAsync(user, roleName);
        return granted is null || granted.Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(User user, string? roleName, IEnumerable<string>? pageAccess, string? homePagePath)
    {
        var existing = (await _pageAccessRepository.GetManyByAsync(p => p.UserId == user.Id)).ToList();

        if (existing.Count > 0)
            await _pageAccessRepository.HardDeleteAsync(existing);

        var restricted = IsPageRestricted(roleName);

        if (restricted)
        {
            var rows = (pageAccess ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => new UserPageAccess { UserId = user.Id, PagePath = p })
                .ToList();

            if (rows.Count > 0)
                await _pageAccessRepository.InsertAsync(rows);
        }

        // La página de inicio se guarda junto a los permisos porque es una de ellas: no tiene
        // sentido mandar a alguien a una página que no puede ver.
        user.HomePagePath = string.IsNullOrWhiteSpace(homePagePath) ? null : homePagePath.Trim();
        user.PageAccessConfigured = restricted;
        await _accountService.UpdateUserAsync(user);
    }
}
