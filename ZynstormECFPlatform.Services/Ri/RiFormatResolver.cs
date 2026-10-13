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

public sealed record TypeFormat(RiHtmlTemplate Template, bool IsDefault);

/// <summary>Cliente (formato propio) → tipo de cliente → PDF por defecto.</summary>
public static class RiFormatResolver
{
    /// <summary>
    /// Formatos disponibles de un tipo: los vinculados más el predeterminado (aunque no tenga
    /// vínculo), sin duplicados y con el predeterminado marcado. Requiere cargar
    /// <see cref="ClientType.TemplateLinks"/> con su plantilla y <see cref="ClientType.RiHtmlTemplate"/>.
    /// </summary>
    public static IReadOnlyList<TypeFormat> FormatsOf(ClientType type)
    {
        var result = new List<TypeFormat>();
        var seen = new HashSet<int>();

        foreach (var link in type.TemplateLinks.Where(l => l.RiHtmlTemplate is not null))
        {
            if (seen.Add(link.RiHtmlTemplate.RiHtmlTemplateId))
            {
                result.Add(new TypeFormat(link.RiHtmlTemplate, link.RiHtmlTemplate.RiHtmlTemplateId == type.RiHtmlTemplateId));
            }
        }

        if (type.RiHtmlTemplate is not null && seen.Add(type.RiHtmlTemplate.RiHtmlTemplateId))
        {
            result.Add(new TypeFormat(type.RiHtmlTemplate, true));
        }

        return result;
    }

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
