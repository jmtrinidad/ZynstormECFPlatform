using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Caché de datos que casi nunca cambian y que cada emisión consultaba a la base de datos:
/// moneda, tipo de e-CF y sucursal principal. TTL de 5 minutos y sin invalidación.
///
/// Nunca se guardan aquí el cliente (su ClientInactive cambia por pagos, recordatorios y
/// ediciones, y un cliente suspendido no debe seguir emitiendo), la API key ni el
/// certificado.
/// </summary>
public sealed class EcfReferenceCache(ICacheService cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> load) where T : class
    {
        var cached = cache.Get<T>(key);

        if (cached is not null)
            return cached;

        var loaded = await load();

        // No se cachea la ausencia: un dato que todavía no existe debe poder aparecer.
        if (loaded is not null)
            cache.Set(key, loaded, Ttl);

        return loaded;
    }
}
