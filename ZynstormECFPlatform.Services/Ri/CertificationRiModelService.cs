using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Renders the Ri (Representación Impresa) PDF for certification documents by dispatching,
/// through <see cref="RiPdfRenderer"/>, to the QuestPDF template matching the document's
/// e-CF type — either for a single document or in bulk as a ZIP.
/// </summary>
public class CertificationRiModelService(
    IClientService clientService,
    StorageContext context) : ICertificationRiModelService
{
    public async Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var doc = await context.Set<CertificationDocument>()
            .Include(d => d.EcfType)
            .Include(d => d.CertificationProcess)
            .FirstOrDefaultAsync(d => d.CertificationProcess.ClientId == client.ClientId && d.ENcfSecuence == ncf)
            ?? throw new InvalidOperationException($"No se encontró el comprobante {ncf} para el cliente.");

        return RiPdfRenderer.Render(ParseEcfType(doc.EcfType.Code), doc.XmlSent);
    }

    public async Task<byte[]> RenderAllZipAsync(string clientGuidId, string webRootPath)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var docs = await context.Set<CertificationDocument>()
            .Include(d => d.EcfType)
            .Include(d => d.CertificationProcess)
            .Where(d => d.CertificationProcess.ClientId == client.ClientId)
            .ToListAsync();

        var zipDir = Path.Combine(webRootPath, "certification_files");
        if (!Directory.Exists(zipDir)) Directory.CreateDirectory(zipDir);

        var zipFileName = $"ri_{client.Rnc}_{Guid.NewGuid():N}.zip";
        var zipPath = Path.Combine(zipDir, zipFileName);

        var faltantes = new List<string>();

        using (var zipStream = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var doc in docs)
            {
                try
                {
                    var pdfBytes = RiPdfRenderer.Render(ParseEcfType(doc.EcfType.Code), doc.XmlSent);

                    var entry = archive.CreateEntry($"{doc.ENcfSecuence}.pdf", CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pdfBytes);
                }
                catch (Exception ex)
                {
                    faltantes.Add($"{doc.ENcfSecuence} (Tipo {doc.EcfType.Code}): {ex.Message}");
                }
            }

            if (faltantes.Count > 0)
            {
                var faltantesEntry = archive.CreateEntry("_faltantes.txt", CompressionLevel.Optimal);
                using var faltantesStream = faltantesEntry.Open();
                using var writer = new StreamWriter(faltantesStream, Encoding.UTF8);
                await writer.WriteAsync(string.Join(Environment.NewLine, faltantes));
            }
        }

        return await File.ReadAllBytesAsync(zipPath);
    }

    /// <summary>
    /// Parses the DGII e-CF numeric type code (e.g. "32", "41") carried by
    /// <see cref="ZynstormECFPlatform.Core.Entities.EcfType.Code"/> into the int consumed by
    /// <see cref="RiPdfRenderer.Render(int, string)"/>.
    /// </summary>
    private static int ParseEcfType(string code) =>
        int.TryParse(code, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
