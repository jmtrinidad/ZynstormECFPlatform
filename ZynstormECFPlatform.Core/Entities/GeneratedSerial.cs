namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Serial emitido por la plataforma. El valor original no se persiste; solo su hash.
/// </summary>
public sealed class GeneratedSerial : BaseEntity
{
    public int GeneratedSerialId { get; set; }

    public string SerialHash { get; set; } = null!;

    /// <summary>Último bloque del serial para poder identificarlo sin exponerlo.</summary>
    public string SerialSuffix { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Indica que el serial ya fue validado y consumido.</summary>
    public bool IsUsed { get; set; }

    public DateTime? UsedAtUtc { get; set; }
}
