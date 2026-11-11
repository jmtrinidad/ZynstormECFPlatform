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
