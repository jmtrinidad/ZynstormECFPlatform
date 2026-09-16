using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>
/// Evaluador de estado activo de un cliente a partir de su entidad ApiKey y Client asociada.
/// Función pura: no toca base de datos ni contexto HTTP.
/// </summary>
public static class ClientStatusEvaluator
{
    public const string KeyNotFoundOrInvalidMessage = "ApiKey no encontrada o no válida.";
    public const string KeyInactiveMessage = "El ApiKey proporcionado se encuentra inactivo.";
    public const string ClientNotFoundOrInactiveMessage = "El cliente asociado a este ApiKey no existe o se encuentra inactivo.";
    public const string PaymentSuspendedMessage = "Cliente suspendido por falta de pago.";
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

        var isPaymentSuspended = client.PaymentSuspendedAtUtc != null;
        var isClientInactive = client.ClientInactive;

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
                NextPaymentDate = client.NextPaymentDate,
                PlanName = client.Plan?.Name,
                IsDgiiProduction = client.IsDgiiProduction,
                IsCertified = client.IsCertified,
                Message = PaymentSuspendedMessage
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
                NextPaymentDate = client.NextPaymentDate,
                PlanName = client.Plan?.Name,
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
            NextPaymentDate = client.NextPaymentDate,
            PlanName = client.Plan?.Name,
            IsDgiiProduction = client.IsDgiiProduction,
            IsCertified = client.IsCertified,
            Message = ClientActiveMessage
        };
    }
}
