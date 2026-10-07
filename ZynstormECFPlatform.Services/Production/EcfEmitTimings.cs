using System.Diagnostics;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Cronómetro por fase de una emisión. Cada <see cref="Mark"/> guarda lo transcurrido desde
/// la marca anterior; el resultado se escribe en el log del documento para medir antes de
/// optimizar.
/// </summary>
public sealed class EcfEmitTimings
{
    private readonly Func<long> _elapsedMilliseconds;
    private readonly List<(string Phase, long Milliseconds)> _laps = [];
    private long _last;

    public EcfEmitTimings(Func<long>? elapsedMilliseconds = null)
    {
        if (elapsedMilliseconds is null)
        {
            var watch = Stopwatch.StartNew();
            elapsedMilliseconds = () => watch.ElapsedMilliseconds;
        }

        _elapsedMilliseconds = elapsedMilliseconds;
    }

    public void Mark(string phase)
    {
        var now = _elapsedMilliseconds();
        _laps.Add((phase, now - _last));
        _last = now;
    }

    public override string ToString()
    {
        var total = _elapsedMilliseconds();
        var phases = _laps.Select(lap => $"{lap.Phase}={lap.Milliseconds}ms");

        return string.Join(", ", phases.Append($"total={total}ms"));
    }
}
