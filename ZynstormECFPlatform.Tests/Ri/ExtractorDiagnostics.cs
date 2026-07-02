using ZynstormECFPlatform.Services.Ri;

namespace ZynstormECFPlatform.Tests.Ri;

// Diagnóstico temporal: corre el extractor sobre el PDF modelo real y vuelca la clasificación.
// Borrar cuando termine el tuning.
public class ExtractorDiagnostics
{
    private const string Dir = @"C:\Users\JOSETR~2\AppData\Local\Temp\claude\--wsl-localhost-Ubuntu-home-dev-projects-ZynstormECF-WorkSpace\c328cb0a-041f-44ac-ba10-9fc308e5d3d8\scratchpad\sdd";

    [Fact]
    public void Dump_RealModel_Classification()
    {
        var bytes = File.ReadAllBytes(Path.Combine(Dir, "modelo.pdf"));
        var r = RiModelExtractor.Extract(bytes);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Success={r.Success}  Warnings={string.Join(" | ", r.Warnings)}");
        if (r.Page != null)
        {
            var p = r.Page;
            sb.AppendLine($"Page {p.WidthPt:0}x{p.HeightPt:0}");
            sb.AppendLine($"--- FIELDS ({p.Fields.Count}) ---");
            foreach (var f in p.Fields)
                sb.AppendLine($"  [{f.FieldKey}] x={f.X:0} y={f.Y:0} size={f.FontSize:0.#} align={f.Align}");
            sb.AppendLine($"--- ITEMS topY={p.Items.TopY:0} rowH={p.Items.RowHeight:0} cols={p.Items.Columns.Count} ---");
            foreach (var c in p.Items.Columns)
                sb.AppendLine($"  col [{c.Field}] x={c.X:0} w={c.Width:0} align={c.Align}");
            sb.AppendLine($"--- QR x={p.Qr.X:0} y={p.Qr.Y:0} size={p.Qr.Size:0} ---");
            sb.AppendLine($"--- STATIC RUNS ({p.StaticRuns.Count}) ---");
            foreach (var s in p.StaticRuns.OrderBy(s => s.Y).ThenBy(s => s.X))
                sb.AppendLine($"  y={s.Y:0} x={s.X:0} sz={s.FontSize:0.#} b={(s.Bold ? 1 : 0)} : \"{s.Text}\"");
        }
        File.WriteAllText(Path.Combine(Dir, "extract-dump.txt"), sb.ToString());
        Assert.True(r.Success);
    }
}
