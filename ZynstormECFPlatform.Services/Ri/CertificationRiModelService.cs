using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Ecf;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Dtos.Ri;
using EcfTypeEntity = ZynstormECFPlatform.Core.Entities.EcfType;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Orchestrates Ri (Representación Impresa) print templates: extracts a layout from a
/// client-provided PDF sample (never persisting the source PDF), generates a reference
/// RI for preview, manages ecfType assignment/confirmation, and renders the final RI for
/// certification documents (single or in bulk as a ZIP).
/// </summary>
public class CertificationRiModelService(
    IClientService clientService,
    IEcfTypeService ecfTypeService,
    StorageContext context) : ICertificationRiModelService
{
    private static readonly JsonSerializerOptions LayoutJsonOptions = new() { PropertyNamingPolicy = null };

    public async Task<RiModelDto> UploadAsync(string clientGuidId, string name, IReadOnlyList<string> ecfTypeCodes, byte[] sourcePdf, string fileName)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var template = new CertificationInvoicePrintTemplate
        {
            ClientId = client.ClientId,
            Name = name,
            FileName = fileName,
            ContentType = "application/pdf",
            GuidId = Guid.NewGuid().ToString(),
            RegisteredAt = DateTime.Now
        };

        var extraction = RiModelExtractor.Extract(sourcePdf);

        if (!extraction.Success || extraction.Layout is null)
        {
            template.Status = CertificationRiTemplateStatus.Failed;
            template.ExtractionWarnings = JsonSerializer.Serialize(extraction.Warnings);
            template.FileData = null;
            template.LayoutJson = null;

            context.Set<CertificationInvoicePrintTemplate>().Add(template);
            await context.SaveChangesAsync();

            return ToDto(template, extraction.Warnings);
        }

        template.LayoutJson = JsonSerializer.Serialize(extraction.Layout, LayoutJsonOptions);
        template.ExtractionWarnings = extraction.Warnings.Count > 0 ? JsonSerializer.Serialize(extraction.Warnings) : null;
        template.Status = CertificationRiTemplateStatus.Extracted;
        template.FileData = RiPdfRenderer.Render(extraction.Layout, SampleRiData(client));

        context.Set<CertificationInvoicePrintTemplate>().Add(template);
        await context.SaveChangesAsync();

        await AssignEcfTypesAsync(template, ecfTypeCodes);
        await context.SaveChangesAsync();

        return ToDto(template, extraction.Warnings);
    }

    public async Task<RiModelDto> UpdateAsync(string templateGuidId, string? name, IReadOnlyList<string>? ecfTypeCodes, bool? confirm, byte[]? newSourcePdf, string? fileName)
    {
        var template = await context.Set<CertificationInvoicePrintTemplate>()
            .Include(t => t.EcfTypes).ThenInclude(e => e.EcfType)
            .Include(t => t.Client)
            .FirstOrDefaultAsync(t => t.GuidId == templateGuidId)
            ?? throw new InvalidOperationException($"Modelo RI {templateGuidId} no encontrado.");

        if (!string.IsNullOrWhiteSpace(name))
        {
            template.Name = name;
        }

        List<string> warnings = DeserializeWarnings(template.ExtractionWarnings);

        if (newSourcePdf is not null && newSourcePdf.Length > 0)
        {
            var extraction = RiModelExtractor.Extract(newSourcePdf);
            warnings = extraction.Warnings;

            if (!extraction.Success || extraction.Layout is null)
            {
                template.Status = CertificationRiTemplateStatus.Failed;
                template.ExtractionWarnings = JsonSerializer.Serialize(warnings);
                template.LayoutJson = null;
                template.FileData = null;
            }
            else
            {
                template.LayoutJson = JsonSerializer.Serialize(extraction.Layout, LayoutJsonOptions);
                template.ExtractionWarnings = warnings.Count > 0 ? JsonSerializer.Serialize(warnings) : null;
                template.Status = CertificationRiTemplateStatus.Extracted;
                template.FileData = RiPdfRenderer.Render(extraction.Layout, SampleRiData(template.Client));
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                template.FileName = fileName;
            }
        }

        if (ecfTypeCodes is not null)
        {
            await AssignEcfTypesAsync(template, ecfTypeCodes);
        }

        if (confirm == true)
        {
            if (template.Status == CertificationRiTemplateStatus.Failed || string.IsNullOrWhiteSpace(template.LayoutJson))
            {
                throw new InvalidOperationException("No se puede confirmar un modelo sin un layout extraído correctamente.");
            }

            template.Status = CertificationRiTemplateStatus.Confirmed;
        }
        else if (confirm == false && template.Status == CertificationRiTemplateStatus.Confirmed)
        {
            template.Status = CertificationRiTemplateStatus.Extracted;
        }

        await context.SaveChangesAsync();

        return ToDto(template, warnings);
    }

    public async Task<List<RiModelListItemDto>> ListByClientAsync(string clientGuidId)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var templates = await context.Set<CertificationInvoicePrintTemplate>()
            .Include(t => t.EcfTypes).ThenInclude(e => e.EcfType)
            .Where(t => t.ClientId == client.ClientId && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return templates.Select(t => new RiModelListItemDto
        {
            GuidId = t.GuidId,
            Name = t.Name,
            Description = t.Description,
            AssignedTypeCodes = t.EcfTypes.Select(e => e.EcfType.Code).OrderBy(c => c).ToList(),
            Status = t.Status.ToString(),
            HasReferenceRi = t.FileData is { Length: > 0 }
        }).ToList();
    }

    public async Task<byte[]?> GetReferenceRiAsync(string templateGuidId)
    {
        var template = await context.Set<CertificationInvoicePrintTemplate>()
            .FirstOrDefaultAsync(t => t.GuidId == templateGuidId)
            ?? throw new InvalidOperationException($"Modelo RI {templateGuidId} no encontrado.");

        return template.FileData;
    }

    public async Task DeleteAsync(string templateGuidId)
    {
        var template = await context.Set<CertificationInvoicePrintTemplate>()
            .Include(t => t.EcfTypes)
            .FirstOrDefaultAsync(t => t.GuidId == templateGuidId)
            ?? throw new InvalidOperationException($"Modelo RI {templateGuidId} no encontrado.");

        context.Set<CertificationInvoicePrintTemplateEcfType>().RemoveRange(template.EcfTypes);
        context.Set<CertificationInvoicePrintTemplate>().Remove(template);
        await context.SaveChangesAsync();
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

        var (layout, _) = await GetConfirmedTemplateAsync(client.ClientId, doc.EcfTypeId, doc.EcfType.Code);

        var data = EcfRiDataMapper.Map(doc.XmlSent, DgiiEnvironment.CerteCF);
        return RiPdfRenderer.Render(layout, data);
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

        var confirmedTemplates = await context.Set<CertificationInvoicePrintTemplate>()
            .Include(t => t.EcfTypes)
            .Where(t => t.ClientId == client.ClientId && t.Status == CertificationRiTemplateStatus.Confirmed)
            .ToListAsync();

        var confirmedTypeIds = confirmedTemplates
            .SelectMany(t => t.EcfTypes.Select(e => e.EcfTypeId))
            .ToHashSet();

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
                if (!confirmedTypeIds.Contains(doc.EcfTypeId))
                {
                    faltantes.Add($"{doc.ENcfSecuence} (Tipo {doc.EcfType.Code}): no hay modelo RI confirmado.");
                    continue;
                }

                try
                {
                    var (layout, _) = await GetConfirmedTemplateAsync(client.ClientId, doc.EcfTypeId, doc.EcfType.Code);
                    var data = EcfRiDataMapper.Map(doc.XmlSent, DgiiEnvironment.CerteCF);
                    var pdfBytes = RiPdfRenderer.Render(layout, data);

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
    /// Builds a representative <see cref="RiData"/> for the reference RI (preview) using
    /// the client's own issuer data, a couple of sample items and a sample QR.
    /// </summary>
    private static RiData SampleRiData(Client client)
    {
        const decimal price1 = 1000m;
        const decimal itbis1 = 180m;
        const decimal price2 = 500m;
        const decimal itbis2 = 90m;

        var subTotal = price1 + price2;
        var itbis = itbis1 + itbis2;

        return new RiData
        {
            Issuer = new Party
            {
                Name = client.Name,
                Document = client.Rnc,
                Address = "CALLE PRINCIPAL #1, SANTO DOMINGO",
                Phone = "809-555-1234"
            },
            Buyer = new Party
            {
                Name = "CONSUMIDOR FINAL",
                Document = string.Empty
            },
            ENcf = "E320000000001",
            TipoeCF = "32",
            FechaEmision = DateTime.Now.ToString("dd-MM-yyyy"),
            FechaFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
            SecurityCode = "ABC123",
            QrUrl = EcfQrUrlBuilder.Build(
                DgiiEnvironment.CerteCF,
                32,
                client.Rnc,
                "",
                "E320000000001",
                DateTime.Now.ToString("dd-MM-yyyy"),
                1000m,
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                "ABC123"),
            Items =
            [
                new RiItem { Description = "Producto de muestra 1", Quantity = 1, Price = price1, Itbis = itbis1, Amount = price1 },
                new RiItem { Description = "Producto de muestra 2", Quantity = 1, Price = price2, Itbis = itbis2, Amount = price2 }
            ],
            Totals = new RiTotals
            {
                SubTotal = subTotal,
                Itbis = itbis,
                Exento = 0m,
                Gravado = subTotal,
                Total = subTotal + itbis
            }
        };
    }

    /// <summary>
    /// Assigns the given ecfType codes to <paramref name="template"/>, reassigning each type
    /// away from any other template of the same client (a type can only be confirmed/assigned
    /// to a single template per client at a time).
    /// </summary>
    private async Task AssignEcfTypesAsync(CertificationInvoicePrintTemplate template, IReadOnlyList<string> ecfTypeCodes)
    {
        var normalizedCodes = ecfTypeCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

        var ecfTypes = new List<EcfTypeEntity>();
        foreach (var code in normalizedCodes)
        {
            var ecfType = await ecfTypeService.GetByAsync(t => t.Code == code)
                ?? throw new InvalidOperationException($"Tipo de e-CF {code} no encontrado.");
            ecfTypes.Add(ecfType);
        }

        var ecfTypeIds = ecfTypes.Select(t => t.EcfTypeId).ToList();

        if (ecfTypeIds.Count > 0)
        {
            // Remove these types from any OTHER template belonging to the same client.
            var otherAssignments = await context.Set<CertificationInvoicePrintTemplateEcfType>()
                .Include(a => a.Template)
                .Where(a => a.CertificationInvoicePrintTemplateId != template.CertificationInvoicePrintTemplateId
                            && a.Template.ClientId == template.ClientId
                            && ecfTypeIds.Contains(a.EcfTypeId))
                .ToListAsync();

            if (otherAssignments.Count > 0)
            {
                context.Set<CertificationInvoicePrintTemplateEcfType>().RemoveRange(otherAssignments);
            }
        }

        // Load current assignments for this template (if it's already persisted).
        var currentAssignments = template.CertificationInvoicePrintTemplateId != 0
            ? await context.Set<CertificationInvoicePrintTemplateEcfType>()
                .Where(a => a.CertificationInvoicePrintTemplateId == template.CertificationInvoicePrintTemplateId)
                .ToListAsync()
            : [];

        var toRemove = currentAssignments.Where(a => !ecfTypeIds.Contains(a.EcfTypeId)).ToList();
        if (toRemove.Count > 0)
        {
            context.Set<CertificationInvoicePrintTemplateEcfType>().RemoveRange(toRemove);
        }

        var existingIds = currentAssignments.Select(a => a.EcfTypeId).ToHashSet();
        foreach (var ecfType in ecfTypes)
        {
            if (!existingIds.Contains(ecfType.EcfTypeId))
            {
                context.Set<CertificationInvoicePrintTemplateEcfType>().Add(new CertificationInvoicePrintTemplateEcfType
                {
                    CertificationInvoicePrintTemplateId = template.CertificationInvoicePrintTemplateId,
                    EcfTypeId = ecfType.EcfTypeId
                });
            }
        }
    }

    /// <summary>
    /// Resolves the <see cref="CertificationRiTemplateStatus.Confirmed"/> template assigned to
    /// <paramref name="ecfTypeId"/> for the given client, and deserializes its layout.
    /// </summary>
    private async Task<(LayoutDescriptor Layout, CertificationInvoicePrintTemplate Template)> GetConfirmedTemplateAsync(int clientId, int ecfTypeId, string ecfTypeCode)
    {
        var template = await context.Set<CertificationInvoicePrintTemplate>()
            .Include(t => t.EcfTypes)
            .Where(t => t.ClientId == clientId
                        && t.Status == CertificationRiTemplateStatus.Confirmed
                        && t.EcfTypes.Any(e => e.EcfTypeId == ecfTypeId))
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No hay modelo confirmado para el tipo {ecfTypeCode}");

        if (string.IsNullOrWhiteSpace(template.LayoutJson))
        {
            throw new InvalidOperationException($"No hay modelo confirmado para el tipo {ecfTypeCode}");
        }

        var layout = JsonSerializer.Deserialize<LayoutDescriptor>(template.LayoutJson, LayoutJsonOptions)
            ?? throw new InvalidOperationException($"El layout del modelo RI para el tipo {ecfTypeCode} es inválido.");

        return (layout, template);
    }

    private static List<string> DeserializeWarnings(string? warningsJson)
    {
        if (string.IsNullOrWhiteSpace(warningsJson)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(warningsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static RiModelDto ToDto(CertificationInvoicePrintTemplate template, List<string> warnings)
    {
        return new RiModelDto
        {
            GuidId = template.GuidId,
            Name = template.Name,
            Description = template.Description,
            AssignedTypeCodes = template.EcfTypes.Select(e => e.EcfType?.Code).Where(c => c is not null).Select(c => c!).OrderBy(c => c).ToList(),
            Status = template.Status.ToString(),
            Warnings = warnings,
            HasReferenceRi = template.FileData is { Length: > 0 }
        };
    }
}
