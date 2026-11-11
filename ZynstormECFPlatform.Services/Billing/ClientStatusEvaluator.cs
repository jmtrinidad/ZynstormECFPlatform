using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>
/// Evaluador de estado activo de un cliente a partir de su entidad ApiKey y Client asociada.
/// Un cliente que pertenece a una cuenta de facturación toma de ella el plan, el próximo pago y la suspensión.
/// Función pura: no toca base de datos ni contexto HTTP.
/// </summary>
public static class ClientStatusEvaluator
{
    public const string KeyNotFoundOrInvalidMessage = "ApiKey no encontrada o no válida.";
    public const string KeyInactiveMessage = "El ApiKey proporcionado se encuentra inactivo.";
    public const string ClientNotFoundOrInactiveMessage = "El cliente asociado a este ApiKey no existe o se encuentra inactivo.";
    public const string PaymentSuspendedMessage = "Cliente suspendido por falta de pago.";
    public const string AccountSuspendedMessage = BillingAccountAccess.SuspendedMessage;
    public const string ClientInactiveMessage = "El cliente se encuentra desactivado. Por favor, comuníquese con soporte.";
    public const string ClientActiveMessage = "Cliente activo.";

    public static ClientActiveStatusDto Evaluate(ApiKey? apiKeyEntity)
    {
        if (apiKeyEntity == null || apiKeyEntity.IsDeleted)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                Message = KeyNotFoundOrInvalidMessage
            };
        }

        if (apiKeyEntity.StatusId != (int)StatusEnum.Active)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                Message = KeyInactiveMessage
            };
        }

        var client = apiKeyEntity.Client;
        if (client == null || client.IsDeleted || client.StatusId != (int)StatusEnum.Active)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                Message = ClientNotFoundOrInactiveMessage
            };
        }

        var account = client.BillingAccount;
        var accountSuspended = BillingAccountAccess.IsSuspended(account);
        var isPaymentSuspended = client.PaymentSuspendedAtUtc != null || accountSuspended;
        var isClientInactive = client.ClientInactive;
        var nextPaymentDate = account != null ? account.NextPaymentDate : client.NextPaymentDate;
        var planName = account != null ? account.Plan?.Name : client.Plan?.Name;

        if (isPaymentSuspended)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                ClientGuid = client.GuidId,
                ClientName = client.Name,
                Rnc = client.Rnc,
                ClientInactive = true,
                PaymentSuspended = true,
                NextPaymentDate = nextPaymentDate,
                PlanName = planName,
                IsDgiiProduction = client.IsDgiiProduction,
                IsCertified = client.IsCertified,
                Message = accountSuspended ? AccountSuspendedMessage : PaymentSuspendedMessage
            };
        }

        if (isClientInactive)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                ClientGuid = client.GuidId,
                ClientName = client.Name,
                Rnc = client.Rnc,
                ClientInactive = true,
                PaymentSuspended = false,
                NextPaymentDate = nextPaymentDate,
                PlanName = planName,
                IsDgiiProduction = client.IsDgiiProduction,
                IsCertified = client.IsCertified,
                Message = ClientInactiveMessage
            };
        }

        return new ClientActiveStatusDto
        {
            IsActive = true,
            ClientGuid = client.GuidId,
            ClientName = client.Name,
            Rnc = client.Rnc,
            ClientInactive = false,
            PaymentSuspended = false,
            NextPaymentDate = nextPaymentDate,
            PlanName = planName,
            IsDgiiProduction = client.IsDgiiProduction,
            IsCertified = client.IsCertified,
            Message = ClientActiveMessage
        };
    }
}
