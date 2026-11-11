using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Regla de acceso por cuenta: una cuenta suspendida bloquea a todos sus clientes.</summary>
public static class BillingAccountAccess
{
    public const string SuspendedMessage = "La cuenta de facturación está suspendida por falta de pago. Comuníquese con su proveedor.";

    public static bool IsSuspended(BillingAccount? account) => account?.PaymentSuspendedAtUtc != null;
}
