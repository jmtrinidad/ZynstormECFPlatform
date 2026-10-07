using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Services.Production;

public static class EcfEmitReplay
{
    /// <summary>
    /// Respuesta de <c>emit</c> cuando el eNCF ya tenía un documento aceptado o en proceso.
    /// Tiene la misma forma que una emisión normal para que la librería la clasifique igual:
    /// Success solo si está aceptado, IsPending si sigue en proceso. El mensaje no usa la
    /// palabra "Rechaz": la librería la toma como señal de rechazo.
    /// </summary>
    public static ReceivedEcfEmissionResultDto ToResult(EcfLookupResponse lookup, int attempt)
    {
        var result = new ReceivedEcfEmissionResultDto
        {
            Replayed = true,
            Attempt = attempt,
            Success = lookup.State == EcfLookupState.Accepted,
            IsPending = lookup.State == EcfLookupState.Pending,
            IsAcceptedConditional = lookup.State == EcfLookupState.AcceptedConditional,
            EcfDocumentId = lookup.EcfDocumentId,
            ENcf = lookup.ENcf,
            EcfType = NcfHelper.TryExtractEcfType(lookup.ENcf, out var ecfType) ? ecfType : 0,
            TrackId = lookup.TrackId ?? string.Empty,
            SecurityCode = lookup.SecurityCode ?? string.Empty,
            SignatureDate = lookup.SignatureDate ?? string.Empty,
            QrUrl = lookup.QrUrl ?? string.Empty,
            Message = Message(lookup)
        };

        return result;
    }

    private static string Message(EcfLookupResponse lookup)
    {
        var track = string.IsNullOrWhiteSpace(lookup.TrackId) ? string.Empty : $" TrackId: {lookup.TrackId}";

        return lookup.State switch
        {
            EcfLookupState.Accepted =>
                $"El comprobante ya estaba aceptado; se devolvió el resultado guardado, sin volver a enviarlo.{track}",
            EcfLookupState.AcceptedConditional =>
                $"El comprobante ya estaba aceptado con observaciones; se devolvió el resultado guardado, sin volver a enviarlo.{track}",
            _ =>
                $"El comprobante ya fue recibido y sigue en proceso; se devolvió su estado actual, sin volver a enviarlo.{track}"
        };
    }
}
