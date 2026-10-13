using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Ri;

public enum RiFormatSource
{
    Default,
    ClientType,
    Client
}

/// <param name="Template">Null cuando <paramref name="Source"/> es Default.</param>
public sealed record ResolvedRiFormat(
    RiFormatSource Source,
    string? ClientTypeName,
    RiHtmlTemplate? Template,
    string? SampleDataJson);

/// <summary>Cliente (formato propio) → tipo de cliente → PDF por defecto.</summary>
public static class RiFormatResolver
{
    public static ResolvedRiFormat Resolve(Client client)
    {
        var type = client.ClientType;

        if (client.RiHtmlTemplate is not null)
        {
            return new ResolvedRiFormat(RiFormatSource.Client, type?.Name, client.RiHtmlTemplate, type?.SampleDataJson);
        }

        if (type?.RiHtmlTemplate is not null)
        {
            return new ResolvedRiFormat(RiFormatSource.ClientType, type.Name, type.RiHtmlTemplate, type.SampleDataJson);
        }

        return new ResolvedRiFormat(RiFormatSource.Default, type?.Name, null, null);
    }
}
