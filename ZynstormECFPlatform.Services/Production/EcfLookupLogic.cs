using System.Linq.Expressions;
using System.Text.Json;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Ecf;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
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

    /// <summary>Documentos de ESE cliente con ese eNCF. Es el límite de seguridad del endpoint.</summary>
    public static Expression<Func<EcfDocument, bool>> ForClient(int clientId, string eNcf) =>
        document => document.ClientId == clientId && document.Ncf == eNcf && !document.IsDeleted;

    /// <summary>Ambiente del QR: el configurado o, si falta o no se reconoce, Production.</summary>
    public static DgiiEnvironment ResolveEnvironment(string? configured) =>
        Enum.TryParse<DgiiEnvironment>(configured, ignoreCase: true, out var environment)
            ? environment
            : DgiiEnvironment.Production;

    public static EcfLookupResponse BuildResponse(
        EcfLookupSource source,
        EcfLookupState state,
        int attempts,
        DgiiEnvironment environment)
    {
        var usable = IsUsable(state);
        var message = StateMessage(state);
        string? securityCode = null;
        string? signatureDate = null;
        string? qrUrl = null;

        if (usable)
        {
            var code = string.IsNullOrWhiteSpace(source.SignedXml)
                ? string.Empty
                : ReceivedEcfProductionService.ExtractSecurityCode(source.SignedXml);

            if (string.IsNullOrWhiteSpace(code))
            {
                message = "El comprobante existe en la plataforma, pero no se pudo leer su XML firmado: no hay código de seguridad ni QR.";
            }
            else
            {
                securityCode = code;

                var date = ReceivedEcfProductionService.ExtractXmlValue(source.SignedXml!, "FechaHoraFirma");
                if (string.IsNullOrWhiteSpace(date))
                    date = source.SignatureDateTime?.ToString("dd-MM-yyyy HH:mm:ss") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(date))
                {
                    message = "El comprobante existe en la plataforma, pero no tiene fecha de firma: no se pudo armar el QR.";
                }
                else
                {
                    signatureDate = date;

                    if (NcfHelper.TryExtractEcfType(source.ENcf, out var ecfType))
                    {
                        qrUrl = EcfQrUrlBuilder.Build(
                            environment,
                            ecfType,
                            source.IssuerRnc,
                            source.CustomerRnc,
                            source.ENcf,
                            source.IssueDateUtc.ToString("dd-MM-yyyy"),
                            source.Total,
                            date,
                            code);
                    }
                }
            }
        }

        return new EcfLookupResponse
        {
            Found = true,
            ENcf = source.ENcf,
            State = state,
            IsUsable = usable,
            TrackId = string.IsNullOrWhiteSpace(source.TrackId) ? null : source.TrackId,
            SecurityCode = securityCode,
            SignatureDate = signatureDate,
            QrUrl = qrUrl,
            EcfDocumentId = source.EcfDocumentId,
            Total = source.Total,
            IssueDate = source.IssueDateUtc,
            Attempts = attempts,
            Message = message
        };
    }

    private static string StateMessage(EcfLookupState state) => state switch
    {
        EcfLookupState.Rejected => "La plataforma tiene este eNCF rechazado.",
        EcfLookupState.Error => "La plataforma tiene este eNCF con error de envío.",
        EcfLookupState.NotSent => "El documento no llegó a salir hacia la DGII; se puede reenviar.",
        _ => string.Empty
    };

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
