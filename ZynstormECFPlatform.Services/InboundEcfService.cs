using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services;

public class InboundEcfService : IInboundEcfService
{
    private readonly IStorageContext _storageContext;

    public InboundEcfService(IStorageContext storageContext)
    {
        _storageContext = storageContext;
    }

    public async Task<string> ReceiveEcfAsync(string xmlContent)
    {
        // For standard reception, DGII dictates assigning a trackId and parsing the RNC and eNCF.
        // We do a fast XML parse using XmlDocument
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        // DGII namespaces often vary in inbound documents from other providers, but they should map to 'ECF' root 
        // or 'eCF'. Often they're signed and have namespace. We'll use GetElementsByTagName to avoid namespace issues.
        
        string rncEmisor = string.Empty;
        var emisorList = doc.GetElementsByTagName("RNCEmisor");
        if (emisorList.Count > 0)
        {
            rncEmisor = emisorList[0]?.InnerText ?? string.Empty;
        }

        string eNcf = string.Empty;
        var ncfList = doc.GetElementsByTagName("eNCF");
        if (ncfList.Count == 0) // Try older standard or different casing
            ncfList = doc.GetElementsByTagName("eNCF", "*");

        if (ncfList.Count > 0)
        {
            eNcf = ncfList[0]?.InnerText ?? string.Empty;
        }

        string trackId = Guid.NewGuid().ToString("N");

        // Use DbContext dynamically via Reflection since IStorageContext interface might not expose IncomingEcfDocuments directly
        // Wait, we can safely cast to StorageContext or assuming IStorageContext has a method.
        // Better yet: _storageContext is known to be Data.StorageContext in implementation usually.
        // For a more loosely coupled approach, we use Add on DbContext directly
        var db = _storageContext as DbContext;
        if (db != null)
        {
            var entity = new IncomingEcfDocument
            {
                RncEmisor = rncEmisor,
                ENcf = eNcf,
                TrackId = trackId,
                ReceivedAtUtc = DateTime.UtcNow,
                RawXml = xmlContent,
                IsCommerciallyApproved = false
            };

            db.Add(entity);
            await db.SaveChangesAsync();
        }

        return trackId;
    }

    public async Task ProcessCommercialApprovalAsync(string xmlContent)
    {
        // When a buyer approves an eCF we sent them, they send an XML with root <AprobacionComercial>.
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        string eNcf = string.Empty;
        var ncfList = doc.GetElementsByTagName("eNCF");
        if (ncfList.Count > 0)
        {
            eNcf = ncfList[0]?.InnerText ?? string.Empty;
        }

        bool approved = false;
        var statusList = doc.GetElementsByTagName("EstadoAprobacion"); // Usually "1" for approved, "2" for rejected
        if (statusList.Count > 0 && statusList[0]?.InnerText == "1")
        {
            approved = true;
        }

        var db = _storageContext as DbContext;
        if (db != null && !string.IsNullOrEmpty(eNcf))
        {
            // Find corresponding EcfDocument that we sent using the "eNcf"
            // Wait, EcfDocument.Ncf stores the document number
            // EF Core Set
            var set = db.Set<EcfDocument>();
            var ecf = await set.FirstOrDefaultAsync(e => e.Ncf == eNcf);
            if (ecf != null)
            {
                // Status mapping: 10 = Accepted, 11 = Rejected
                ecf.EcfStatusId = approved ? 10 : 11;
                await db.SaveChangesAsync();
            }
        }
    }
}
