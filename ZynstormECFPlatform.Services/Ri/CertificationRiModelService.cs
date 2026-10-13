using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Services.Ri.Html;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Renders the Ri (Representación Impresa) PDF for certification documents by dispatching,
/// through <see cref="RiPdfRenderer"/>, to the QuestPDF template matching the document's
/// e-CF type — either for a single document or in bulk as a ZIP.
/// </summary>
public class CertificationRiModelService(
    IClientService clientService,
    StorageContext context,
    RiFormatService formatService) : ICertificationRiModelService
{
    public async Task<RiRenderResult> RenderRiAsync(string clientGuidId, string ncf)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var format = await formatService.ResolveForClientAsync(client.ClientId);
        if (format.Template is null)
        {
            // Sin formato HTML: comportamiento original (PDF QuestPDF).
            return new RiRenderResult(await RenderRiForDocumentAsync(clientGuidId, ncf), "application/pdf", $"RI_{ncf}.pdf");
        }

        var doc = await context.Set<CertificationDocument>()
            .Include(d => d.CertificationProcess)
            .FirstOrDefaultAsync(d => d.CertificationProcess.ClientId == client.ClientId && d.ENcfSecuence == ncf)
            ?? throw new InvalidOperationException($"No se encontró el comprobante {ncf} para el cliente.");

        var company = await BuildCompanyHeaderAsync(client);
        var html = RiHtmlRenderer.Render(format.Template.Html, doc.XmlSent, company, format.SampleDataJson);
        return new RiRenderResult(Encoding.UTF8.GetBytes(html), "text/html", $"RI_{ncf}.html");
    }

    public async Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var doc = await context.Set<CertificationDocument>()
            .Include(d => d.EcfType)
            .Include(d => d.CertificationProcess)
            .FirstOrDefaultAsync(d => d.CertificationProcess.ClientId == client.ClientId && d.ENcfSecuence == ncf)
            ?? throw new InvalidOperationException($"No se encontró el comprobante {ncf} para el cliente.");

        var company = await BuildCompanyHeaderAsync(client);
        return RiPdfRenderer.Render(ParseEcfType(doc.EcfType.Code), doc.XmlSent, company);
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

        var company = await BuildCompanyHeaderAsync(client);
        var format = await formatService.ResolveForClientAsync(client.ClientId);
        var faltantes = new List<string>();

        using (var zipStream = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var doc in docs)
            {
                try
                {
                    byte[] bytes;
                    string extension;
                    if (format.Template is null)
                    {
                        bytes = RiPdfRenderer.Render(ParseEcfType(doc.EcfType.Code), doc.XmlSent, company);
                        extension = "pdf";
                    }
                    else
                    {
                        bytes = Encoding.UTF8.GetBytes(RiHtmlRenderer.Render(format.Template.Html, doc.XmlSent, company, format.SampleDataJson));
                        extension = "html";
                    }

                    var entry = archive.CreateEntry($"{doc.ENcfSecuence}.{extension}", CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(bytes);
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
    /// Encabezado del emisor tomado del CLIENTE seleccionado (no del XML): nombre/RNC/teléfono
    /// del Client, dirección de Client.Address con fallback a la sucursal principal
    /// (ClientBranche), y WhatsApp = mismo teléfono.
    /// </summary>
    private async Task<RiCompanyHeader> BuildCompanyHeaderAsync(Client client)
    {
        var branch = await context.Set<ClientBranche>()
            .Where(b => b.ClientId == client.ClientId)
            .OrderByDescending(b => b.IsMain)
            .FirstOrDefaultAsync();

        var phone = !string.IsNullOrWhiteSpace(client.Phone) ? client.Phone! : branch?.Phone ?? string.Empty;

        return new RiCompanyHeader(
            Name: client.Name,
            Rnc: client.Rnc,
            Address: !string.IsNullOrWhiteSpace(client.Address) ? client.Address! : branch?.Address ?? string.Empty,
            Phone: phone,
            Whatsapp: phone);
    }

    /// <summary>
    /// Parses the DGII e-CF numeric type code (e.g. "32", "41") carried by
    /// <see cref="ZynstormECFPlatform.Core.Entities.EcfType.Code"/> into the int consumed by
    /// <see cref="RiPdfRenderer.Render(int, string)"/>.
    /// </summary>
    private static int ParseEcfType(string code) =>
        int.TryParse(code, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
