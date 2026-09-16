using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentRegistrationPolicyTests
{
    private static readonly DateTime Today = new(2026, 9, 18);

    [Fact]
    public void NewNextPaymentDate_AdvancesFromDueDate_NotFromPaymentDate()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2026, 9, 15), new DateTime(2026, 9, 18), 1);
        Assert.Equal(new DateTime(2026, 10, 15), next);
    }

    [Fact]
    public void NewNextPaymentDate_AdvancesByPaidMonths()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2026, 9, 15), new DateTime(2026, 9, 1), 12);
        Assert.Equal(new DateTime(2027, 9, 15), next);
    }

    [Fact]
    public void NewNextPaymentDate_WithoutPreviousDate_StartsFromPaymentDate()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(null, new DateTime(2026, 9, 18), 3);
        Assert.Equal(new DateTime(2026, 12, 18), next);
    }

    [Fact]
    public void NewNextPaymentDate_EndOfMonth_ClampsToLastDay()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2027, 1, 31), new DateTime(2027, 2, 1), 1);
        Assert.Equal(new DateTime(2027, 2, 28), next);
    }

    [Fact]
    public void NewNextPaymentDate_ZeroMonths_TreatedAsOne()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2026, 9, 15), Today, 0);
        Assert.Equal(new DateTime(2026, 10, 15), next);
    }

    [Theory]
    [InlineData(2026, 8, true)]
    [InlineData(2025, 12, true)]
    [InlineData(2026, 9, false)]
    [InlineData(2026, 10, false)]
    public void OverageMonthPayable_OnlyClosedMonths(int year, int month, bool expected)
    {
        Assert.Equal(expected, PaymentRegistrationPolicy.IsOverageMonthPayable(year, month, Today));
    }

    [Fact]
    public void Reactivate_WhenSuspendedAndPlanFeeBringsClientCurrent()
    {
        Assert.True(PaymentRegistrationPolicy.ShouldReactivate(true, true, new DateTime(2026, 10, 15), Today));
    }

    [Fact]
    public void Reactivate_NotWhenStillOverdueAfterPayment()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(true, true, new DateTime(2026, 8, 15), Today));
    }

    [Fact]
    public void Reactivate_NotWhenNextPaymentIsToday()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(true, true, Today, Today));
    }

    [Fact]
    public void Reactivate_NotForOverageOnlyPayment()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(true, false, new DateTime(2027, 1, 1), Today));
    }

    [Fact]
    public void Reactivate_NotWhenNotSuspended()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(false, true, new DateTime(2026, 10, 15), Today));
    }

    [Fact]
    public void ValidateRequest_ValidPlanFeeRequest_HasNoErrors()
    {
        Assert.Empty(PaymentRegistrationPolicy.ValidateRequest(Today, 2, true, 0, "TRX-1", null, Today));
    }

    [Fact]
    public void ValidateRequest_OverageOnly_IsValid()
    {
        Assert.Empty(PaymentRegistrationPolicy.ValidateRequest(Today, 1, false, 2, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_MissingDate_ReturnsError()
    {
        Assert.Contains("La fecha del pago es requerida.", PaymentRegistrationPolicy.ValidateRequest(null, 1, true, 0, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_FutureDate_ReturnsError()
    {
        Assert.Contains("La fecha del pago no puede ser futura.",
            PaymentRegistrationPolicy.ValidateRequest(Today.AddDays(1), 1, true, 0, null, null, Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ValidateRequest_InvalidMethod_ReturnsError(int method)
    {
        Assert.Contains("El método de pago no es válido.",
            PaymentRegistrationPolicy.ValidateRequest(Today, method, true, 0, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_NothingSelected_ReturnsError()
    {
        Assert.Contains("Debe seleccionar la mensualidad o al menos un excedente.",
            PaymentRegistrationPolicy.ValidateRequest(Today, 1, false, 0, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_TooLongReferenceAndNotes_ReturnErrors()
    {
        var errors = PaymentRegistrationPolicy.ValidateRequest(Today, 1, true, 0, new string('x', 101), new string('x', 501), Today);
        Assert.Contains("La referencia no puede exceder 100 caracteres.", errors);
        Assert.Contains("La nota no puede exceder 500 caracteres.", errors);
    }

    [Fact]
    public void ReceiptNumber_IsPaddedToSixDigits()
    {
        Assert.Equal("REC-000042", PaymentRegistrationPolicy.FormatReceiptNumber(42));
    }
}
