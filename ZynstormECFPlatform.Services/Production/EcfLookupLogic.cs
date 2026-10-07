using System.Text.Json;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

public static class EcfLookupLogic
{
    /// <summary>eNCF en mayúsculas y sin espacios, o null si no es un eNCF válido.</summary>
    public static string? NormalizeNcf(string? raw)
    {
        var value = raw?.Trim().ToUpperInvariant();

        if (value is null || value.Length != 13 || value[0] != 'E')
            return null;

        if (!value.Skip(1).All(char.IsAsciiDigit))
            return null;

        return NcfHelper.TryExtractEcfType(value, out _) ? value : null;
    }

    /// <summary>
    /// Estado a partir del EcfStatusId de la plataforma. El id 11 se usa tanto para Rejected
    /// como para Aceptado Condicional (MapDgiiStatusToEcfStatus), así que ahí se lee el
    /// estado de la última transmisión.
    /// </summary>
    public static EcfLookupState ResolveState(int ecfStatusId, string? lastResponsePayload) => ecfStatusId switch
    {
        10 => EcfLookupState.Accepted,
        7 or 8 or 9 => EcfLookupState.Pending,
        11 => IsConditional(lastResponsePayload) ? EcfLookupState.AcceptedConditional : EcfLookupState.Rejected,
        3 => EcfLookupState.Rejected,
        12 => EcfLookupState.Error,
        _ => EcfLookupState.NotSent
    };

    public static bool IsUsable(EcfLookupState state) =>
        state is EcfLookupState.Accepted or EcfLookupState.AcceptedConditional or EcfLookupState.Pending;

    public static EcfLookupCandidate? PickBest(IEnumerable<EcfLookupCandidate> candidates) =>
        candidates
            .OrderBy(candidate => Priority(candidate.State))
            .ThenByDescending(candidate => candidate.RegisteredAt)
            .ThenByDescending(candidate => candidate.EcfDocumentId)
            .FirstOrDefault();

    private static int Priority(EcfLookupState state) => state switch
    {
        EcfLookupState.Accepted => 0,
        EcfLookupState.AcceptedConditional => 1,
        EcfLookupState.Pending => 2,
        EcfLookupState.Rejected => 3,
        EcfLookupState.Error => 4,
        _ => 5
    };

    /// <summary>
    /// ResponsePayload se guarda como { transmission, status }. Solo importa si el estado de
    /// la DGII fue "Aceptado Condicional".
    /// </summary>
    private static bool IsConditional(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("status", out var statusElement)
                || statusElement.ValueKind != JsonValueKind.Object)
                return false;

            var status = statusElement.Deserialize<DgiiStatusResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return status is not null && ReceivedEcfProductionService.IsAcceptedConditionalDgiiStatus(status);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
