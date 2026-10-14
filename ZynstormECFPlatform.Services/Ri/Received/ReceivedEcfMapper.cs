using System.Globalization;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri.Received;

/// <summary>
/// Convierte el XML firmado de un e-CF recibido en el <see cref="ReceivedEcfModel"/> del PDF.
/// Reutiliza <see cref="EcfRiDataMapper"/> (emisor, comprador, ítems, totales, QR) y los helpers
/// de <see cref="EcfRiTemplateMapper"/> para que las etiquetas coincidan con las del resto de RI.
/// </summary>
public static class ReceivedEcfMapper
{
    public static ReceivedEcfModel Map(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        var ecfType = int.TryParse(data.TipoeCF, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        return new ReceivedEcfModel
        {
            EcfType = ecfType,
            TypeName = EcfRiTemplateMapper.NcfTypeName(ecfType),
            ENcf = data.ENcf,
            Issuer = data.Issuer,
            Buyer = data.Buyer,
            FechaEmision = data.FechaEmision.Replace('-', '/'),
            FechaFirma = data.FechaFirma,
            ValidUntil = EcfRiTemplateMapper.FormatDate(data.FechaVencimientoSecuencia),
            PaymentType = EcfRiTemplateMapper.PaymentTypeLabel(data.TipoPago),
            PaymentCondition = EcfRiTemplateMapper.PaymentCondition(data),
            InternalInvoiceNumber = data.NumeroFacturaInterna,
            AffectedNcf = data.NcfModificado,
            ModificationCode = EcfRiTemplateMapper.ModificationCodeLabel(data.CodigoModificacion),
            ModificationReason = data.RazonModificacion,
            Items = data.Items.ConvertAll(item => new ReceivedEcfItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = EcfRiTemplateMapper.UnitAbbreviation(item.UnidadMedida),
                Price = item.Price,
                Discount = item.Discount,
                // Los XML de la DGII no siempre traen MontoITBIS por ítem: se deriva de la tasa.
                Itbis = item.Itbis > 0
                    ? item.Itbis
                    : Math.Round(item.Amount * EcfRiTemplateMapper.ItbisRateFor(item.IndicadorFacturacion, data.Totals) / 100m, 2),
                Amount = item.Amount
            }),
            SubTotal = data.Totals.SubTotal,
            Discount = data.Items.Sum(item => item.Discount),
            Exento = data.Totals.Exento,
            Itbis = data.Totals.Itbis,
            ItbisRate = data.Totals.Itbis1Rate,
            ItbisRetenido = data.Totals.ItbisRetenido,
            IsrRetencion = data.Totals.IsrRetencion,
            Total = data.Totals.Total,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }
}
