using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IDgiiRncService
{
    Task<DgiiRncStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task StartDownloadAndIndexingAsync(CancellationToken ct = default);

    /// <summary>
    /// Descarga e indexa el padrón en línea, sin devolver el control hasta terminar.
    /// Lanza si falla, para que el job que la invoca se entere y pueda reintentar.
    /// Devuelve false si ya había otra corrida en curso.
    /// </summary>
    Task<bool> RunDownloadAndIndexingAsync(CancellationToken ct = default);

    Task<DgiiRncSearchResultDto> LookupRncAsync(string rnc, CancellationToken ct = default);
}
