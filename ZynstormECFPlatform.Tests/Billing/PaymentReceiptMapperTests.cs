using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentReceiptMapperTests
{
    private static ClientPayment SamplePayment() => new()
    {
        ClientPaymentId = 12,
        PaymentDate = new DateTime(2026, 11, 2),
        PaymentMethod = 2,
        TotalAmount = 3300m,
        Items =
        {
            new ClientPaymentItem
            {
                ItemType = 1, Amount = 3000m, PlanName = "Pool 1000", MonthsCovered = 1,
                NewNextPaymentDate = new DateTime(2026, 11, 15)
            },
            new ClientPaymentItem
            {
                ItemType = 2, Amount = 300m, PlanName = "Pool 1000", Year = 2026, Month = 10, OverageDocuments = 50
            }
        }
    };

    [Fact]
    public void ToReceipt_ForAnAccount_FillsThePayerAndTheAccountFlag()
    {
        var receipt = PaymentReceiptMapper.ToReceipt(SamplePayment(), "acct-guid", "Mi Cuenta", isAccount: true);

        Assert.Equal("REC-000012", receipt.ReceiptNumber);
        Assert.True(receipt.IsAccount);
        Assert.Equal("acct-guid", receipt.ClientGuidId);
        Assert.Equal("Mi Cuenta", receipt.ClientName);
        Assert.Equal(3300m, receipt.TotalAmount);
        Assert.Equal(new DateTime(2026, 11, 15), receipt.NextPaymentDate);
    }

    [Fact]
    public void ToReceipt_ForAClient_IsNotMarkedAsAccount()
    {
        var receipt = PaymentReceiptMapper.ToReceipt(SamplePayment(), "client-guid", "Cliente");

        Assert.False(receipt.IsAccount);
    }

    [Fact]
    public void ToReceipt_DescribesTheFeeAndTheOverage()
    {
        var receipt = PaymentReceiptMapper.ToReceipt(SamplePayment(), "g", "n");

        Assert.Equal("Mensualidad Pool 1000 (1 mes)", receipt.Items[0].Description);
        Assert.Equal("Excedente Octubre 2026 (50 comprobantes)", receipt.Items[1].Description);
    }
}
