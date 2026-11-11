using System.Linq.Expressions;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Billing;

public static class BillingScope
{
    /// <summary>
    /// Clientes que se cobran por su propio plan (pagos, recordatorios y suspensión individuales).
    /// Los que pertenecen a una cuenta de facturación se cobran por la cuenta, aunque conserven un PlanId.
    /// </summary>
    public static readonly Expression<Func<Client, bool>> IndividuallyBilled = c => c.BillingAccountId == null;
}
