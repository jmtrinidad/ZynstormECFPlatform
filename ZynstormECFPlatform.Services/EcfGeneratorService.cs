using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services;

public class EcfGeneratorService : IEcfGeneratorService
{
    private static readonly XmlSerializerNamespaces _namespaces;

    static EcfGeneratorService()
    {
        _namespaces = new XmlSerializerNamespaces();
        _namespaces.Add("", "");
    }

    public string GenerateUnsignedXml(EcfInvoiceRequestDto dto)
    {
        var document = MapToEntity(dto);

        var serializer = new XmlSerializer(typeof(EcfDocument));
        
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        using var memoryStream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
        {
            serializer.Serialize(xmlWriter, document, _namespaces);
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    public List<string> ValidateDto(EcfInvoiceRequestDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Ncf)) errors.Add("NCF is required.");
        if (dto.Items.Count == 0) errors.Add("At least one item is required.");

        return errors;
    }

    private EcfDocument MapToEntity(EcfInvoiceRequestDto dto)
    {
        var document = new EcfDocument
        {
            EcfTypeId = dto.EcfTypeId,
            Ncf = dto.Ncf,
            ExternalReference = dto.ExternalReference,
            IssueDateUtc = dto.IssueDate,
            SequenceExpirationDate = dto.SequenceExpirationDate ?? DateTime.UtcNow.AddYears(1),
            CustomerRnc = dto.CustomerRnc,
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            CustomerAddress = dto.CustomerAddress,
            PaymentType = dto.PaymentType,
            PaymentDeadline = dto.PaymentDeadline,
            PaymentTerms = dto.PaymentTerms,
            Version = "1.0"
        };

        decimal totalBase = 0;
        decimal totalItemDiscounts = 0;
        decimal totalItbis = 0;
        decimal totalExempt = 0;

        // Montos gravados por tasa (G1: 18%, G2: 16%, G3: 0%)
        decimal taxableG1 = 0;
        decimal taxableG2 = 0;
        decimal taxableG3 = 0;
        decimal itbisG1 = 0;
        decimal itbisG2 = 0;
        decimal itbisG3 = 0;

        var lineNo = 1;
        foreach (var item in dto.Items)
        {
            var baseAmount = Math.Round(item.Quantity * item.UnitPrice, 2);
            var discountAmount = Math.Round(item.Discount, 2);
            var taxableAmount = baseAmount - discountAmount;
            
            var itbisAmount = item.ItbisAmount > 0 
                ? item.ItbisAmount 
                : Math.Round(taxableAmount * (item.TaxPercentage / 100), 2);

            document.EcfDocumentDetails.Add(new EcfDocumentDetail
            {
                LineNumber = lineNo++,
                ItemName = item.Name,
                Description = item.Description ?? item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = discountAmount,
                ItbisPercentage = item.TaxPercentage,
                ItbisAmount = itbisAmount,
                ItemType = item.ItemType,
                BillingIndicator = 1,
                UnitOfMeasure = item.UnitOfMeasure ?? 1,
                ItemAmount = taxableAmount + itbisAmount
            });

            totalBase += baseAmount;
            totalItemDiscounts += discountAmount;
            totalItbis += itbisAmount;

            if (item.TaxPercentage == 0)
            {
                totalExempt += taxableAmount;
                taxableG3 += taxableAmount;
            }
            else if (item.TaxPercentage == 18)
            {
                taxableG1 += taxableAmount;
                itbisG1 += itbisAmount;
            }
            else if (item.TaxPercentage == 16)
            {
                taxableG2 += taxableAmount;
                itbisG2 += itbisAmount;
            }
        }

        // Manejo de Descuento Global
        if (dto.GlobalDiscountAmount > 0)
        {
            document.EcfGlobalAdjustments.Add(new EcfGlobalAdjustment
            {
                LineNumber = 1,
                AdjustmentType = "D",
                Description = dto.GlobalDiscountDescription ?? "Descuento Global",
                ValueType = "$",
                Amount = dto.GlobalDiscountAmount
            });
        }

        var totalBeforeGlobalDiscount = (totalBase - totalItemDiscounts) + totalItbis;
        var finalTotal = totalBeforeGlobalDiscount - dto.GlobalDiscountAmount;

        // Populación del Header principal
        document.SubTotal = totalBase;
        document.Total = finalTotal;
        document.Itbistotal = totalItbis;

        // Populación del Bloque de Totales (EcfDocumentTotal)
        var totalEntity = new EcfDocumentTotal
        {
            TaxableAmount = totalBase - totalItemDiscounts - totalExempt,
            TaxableTotal = totalBase - totalItemDiscounts,
            DiscountTotal = totalItemDiscounts + dto.GlobalDiscountAmount,
            ITBISTotal = totalItbis,
            ExemptTotal = totalExempt,
            Total = finalTotal,
            
            // Desglose por tasas (Standard DGII)
            TaxableAmountG1 = taxableG1 > 0 ? taxableG1 : null,
            TaxAmount1 = itbisG1 > 0 ? itbisG1 : null,
            TaxRate1 = taxableG1 > 0 ? 18 : null,

            TaxableAmountG2 = taxableG2 > 0 ? taxableG2 : null,
            TaxAmount2 = itbisG2 > 0 ? itbisG2 : null,
            TaxRate2 = taxableG2 > 0 ? 16 : null,

            TaxableAmountG3 = taxableG3 > 0 ? taxableG3 : null,
            TaxAmount3 = itbisG3 > 0 ? itbisG3 : null,
            TaxRate3 = taxableG3 > 0 ? 0 : null
        };

        document.EcfDocumentTotals.Add(totalEntity);

        return document;
    }
}
