using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Reglas para crear y editar cuentas de facturación. Funciones puras.</summary>
public static class BillingAccountPolicy
{
    public const int NameMaxLength = 100;
    public const int MaxPaidMonths = 60;

    public static List<string> ValidateAccount(
        string? name,
        Plan? plan,
        decimal? customMonthlyFee,
        int paidMonths,
        decimal prepaymentDiscountPercent,
        int paymentGraceDays)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("El nombre de la cuenta es requerido.");
        else if (name.Trim().Length > NameMaxLength)
            errors.Add($"El nombre no puede exceder {NameMaxLength} caracteres.");

        if (plan == null)
            errors.Add("El plan seleccionado no existe.");
        else
        {
            if (plan.PlanTypeId != (int)PlanTypeEnum.Documents)
                errors.Add("Solo los planes por comprobantes se pueden asignar a una cuenta.");
            if (plan.StatusId != (int)StatusEnum.Active)
                errors.Add("El plan seleccionado no está activo.");
        }

        if (customMonthlyFee < 0)
            errors.Add("El precio propio no puede ser negativo.");

        if (paidMonths < 1 || paidMonths > MaxPaidMonths)
            errors.Add($"Los meses a pagar deben estar entre 1 y {MaxPaidMonths}.");

        if (prepaymentDiscountPercent < 0 || prepaymentDiscountPercent > 100)
            errors.Add("El descuento por pago adelantado debe estar entre 0 y 100.");

        if (paymentGraceDays < 1 || paymentGraceDays > PaymentReminderPolicy.MaxGraceDays)
            errors.Add($"Los días de gracia deben estar entre 1 y {PaymentReminderPolicy.MaxGraceDays}.");

        return errors;
    }

    public static List<string> ValidateOwner(bool ownerExists, bool ownerIsAdmin, bool ownerAlreadyHasAccount)
    {
        var errors = new List<string>();

        if (!ownerExists)
            errors.Add("El usuario dueño no existe.");
        else
        {
            if (!ownerIsAdmin)
                errors.Add("El dueño de la cuenta debe tener el rol Admin.");
            if (ownerAlreadyHasAccount)
                errors.Add("El usuario ya tiene una cuenta de facturación.");
        }

        return errors;
    }

    /// <summary>Los clientes a asignar deben existir y estar libres o ya en esta misma cuenta.</summary>
    public static List<string> ValidateMembers(IReadOnlyCollection<Client> found, int requestedCount, int accountId)
    {
        var errors = new List<string>();

        if (found.Count != requestedCount)
            errors.Add("Alguno de los clientes seleccionados no existe.");

        foreach (var client in found.Where(c => c.BillingAccountId != null && c.BillingAccountId != accountId))
            errors.Add($"El cliente {client.Name} ya pertenece a otra cuenta.");

        return errors;
    }

    public static List<string> ValidateQuota(int? quota) =>
        quota < 0 ? ["El cupo no puede ser negativo."] : [];

    /// <summary>El cupo es informativo: solo sirve para mostrar una alerta, nunca bloquea la emisión.</summary>
    public static bool IsQuotaExceeded(int acceptedDocuments, int? quota) =>
        quota is int limit && acceptedDocuments > limit;
}
