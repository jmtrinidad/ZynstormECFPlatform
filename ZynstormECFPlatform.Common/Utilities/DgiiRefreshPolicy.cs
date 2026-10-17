namespace ZynstormECFPlatform.Common.Utilities;

/// <summary>
/// Decide si al arrancar hay que volver a descargar el padrón de la DGII. Vive aparte del
/// servicio de arranque para poder probarla sin levantar el host.
///
/// Cubre el caso de un contenedor que se reinicia cruzando la medianoche: el job diario de
/// ese día no corrió y nadie se entera.
/// </summary>
public static class DgiiRefreshPolicy
{
    /// <summary>
    /// Días de antigüedad a partir de los cuales el padrón se considera vencido. Dos y no
    /// uno para que un arranque a las 00:05, minutos después de que corrió el job, no
    /// dispare una segunda descarga de 60 MB.
    /// </summary>
    public const int DefaultMaxAgeDays = 2;

    public static bool ShouldRefreshOnStartup(
        long totalRecords,
        DateTime? lastUpdated,
        DateTime now,
        int maxAgeDays)
    {
        // Sin registros no hay padrón: hay que bajarlo sí o sí.
        if (totalRecords <= 0) return true;

        // Hay índice pero no se sabe de cuándo: se trata como vencido.
        if (lastUpdated is null) return true;

        // Cero o menos apaga el vencimiento por edad y deja solo el caso "no hay padrón".
        if (maxAgeDays <= 0) return false;

        return lastUpdated.Value.AddDays(maxAgeDays) <= now;
    }
}
