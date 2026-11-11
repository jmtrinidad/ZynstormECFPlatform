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
