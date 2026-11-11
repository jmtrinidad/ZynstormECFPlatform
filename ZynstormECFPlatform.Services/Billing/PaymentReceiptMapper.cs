using System.Globalization;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Convierte un recibo guardado en su DTO. Sirve para recibos de clientes sueltos y de cuentas.</summary>
public static class PaymentReceiptMapper
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    public static PaymentReceiptDto ToReceipt(ClientPayment payment, string payerGuid, string payerName, bool isAccount = false) => new()
    {
        ClientPaymentId = payment.ClientPaymentId,
        ReceiptNumber = PaymentRegistrationPolicy.FormatReceiptNumber(payment.ClientPaymentId),
        ClientGuidId = payerGuid,
        ClientName = payerName,
        IsAccount = isAccount,
        PaymentDate = payment.PaymentDate,
        PaymentMethod = payment.PaymentMethod,
        Reference = payment.Reference,
        Notes = payment.Notes,
        TotalAmount = payment.TotalAmount,
        NextPaymentDate = payment.Items
            .FirstOrDefault(i => i.ItemType == (int)ClientPaymentItemType.PlanFee)?.NewNextPaymentDate,
        Items = payment.Items
            .OrderBy(i => i.ItemType)
            .ThenBy(i => i.Year)
            .ThenBy(i => i.Month)
            .Select(i => new PaymentReceiptItemDto
            {
                ItemType = i.ItemType,
                Description = DescribeItem(i),
                Amount = i.Amount,
                MonthsCovered = i.MonthsCovered,
                DiscountAmount = i.DiscountAmount,
                OfferMonthsCovered = HasOffer(i) ? i.OfferMonthsCovered : null,
                OfferUnitPrice = HasOffer(i) ? i.OfferUnitPrice : null,
                FullPriceMonthsCovered = HasOffer(i) ? i.MonthsCovered - i.OfferMonthsCovered : null,
                FullPriceUnitPrice = HasOffer(i)
                    ? OfferPricing.GetFullPriceUnit(i.GrossAmount ?? i.Amount, i.MonthsCovered!.Value, i.OfferMonthsCovered!.Value, i.OfferUnitPrice!.Value)
                    : null,
                PreviousNextPaymentDate = i.PreviousNextPaymentDate,
                NewNextPaymentDate = i.NewNextPaymentDate,
                Year = i.Year,
                Month = i.Month,
                OverageDocuments = i.OverageDocuments
            })
            .ToList()
    };

    private static bool HasOffer(ClientPaymentItem item) =>
        item is { ItemType: (int)ClientPaymentItemType.PlanFee, OfferMonthsCovered: > 0, OfferUnitPrice: not null, MonthsCovered: not null };

    private static string DescribeItem(ClientPaymentItem item)
    {
        if (item.ItemType == (int)ClientPaymentItemType.PlanFee)
        {
            var months = item.MonthsCovered == 1 ? "1 mes" : $"{item.MonthsCovered} meses";
            return $"Mensualidad {item.PlanName} ({months})";
        }

        var period = item.Year is int year && item.Month is int month
            ? Es.TextInfo.ToTitleCase(new DateTime(year, month, 1).ToString("MMMM yyyy", Es))
            : string.Empty;
        return $"Excedente {period} ({item.OverageDocuments} comprobantes)";
    }
}
