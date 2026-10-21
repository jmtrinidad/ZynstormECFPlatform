using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class OfferPricingTests
{
    private static readonly DateTime Start = new(2026, 10, 21);
    private static readonly OfferTerms ThreeMonths = new(1500m, 3, Start);

    [Fact]
    public void PlanHasOffer_TrueWithPriceAndPositiveMonths()
    {
        Assert.True(OfferPricing.PlanHasOffer(1500m, 3));
    }

    [Fact]
    public void PlanHasOffer_FalseWhenAnyPartIsMissingOrMonthsAreZero()
    {
        Assert.False(OfferPricing.PlanHasOffer(null, null));
        Assert.False(OfferPricing.PlanHasOffer(1500m, null));
        Assert.False(OfferPricing.PlanHasOffer(null, 3));
        Assert.False(OfferPricing.PlanHasOffer(1500m, 0));
    }

    [Fact]
    public void TermsFor_BuildsTermsAndDropsTheTimeOfDay()
    {
        var terms = OfferPricing.TermsFor(1500m, 3, new DateTime(2026, 10, 21, 15, 30, 0));

        Assert.NotNull(terms);
        Assert.Equal(1500m, terms!.OfferPrice);
        Assert.Equal(3, terms.OfferMonths);
        Assert.Equal(new DateTime(2026, 10, 21), terms.StartDate);
    }

    [Fact]
    public void TermsFor_NullWithoutStartDateOrWithoutPlanOffer()
    {
        Assert.Null(OfferPricing.TermsFor(1500m, 3, null));
        Assert.Null(OfferPricing.TermsFor(null, null, Start));
    }

    [Fact]
    public void EndDate_AddsTheOfferMonthsAndClampsToTheLastDayOfMonth()
    {
        Assert.Equal(new DateTime(2027, 1, 21), ThreeMonths.EndDate);
        Assert.Equal(new DateTime(2027, 2, 28), new OfferTerms(1500m, 3, new DateTime(2026, 11, 30)).EndDate);
    }

    [Fact]
    public void Evaluate_WithoutTerms_IsNone()
    {
        var state = OfferPricing.Evaluate(null, Start);

        Assert.Equal(OfferStatus.None, state.Status);
        Assert.Null(state.EndDate);
        Assert.Null(state.DaysRemaining);
        Assert.Null(state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_OnTheStartDay_IsActiveWithAllTheTimeLeft()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, Start);

        Assert.Equal(OfferStatus.Active, state.Status);
        Assert.Equal(new DateTime(2027, 1, 21), state.EndDate);
        Assert.Equal(92, state.DaysRemaining);
        Assert.Equal(3, state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_MidOffer_CountsWholeMonthsAndDays()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, new DateTime(2026, 11, 30));

        Assert.Equal(OfferStatus.Active, state.Status);
        Assert.Equal(52, state.DaysRemaining);
        Assert.Equal(1, state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_LastDayBeforeTheEnd_IsActiveWithOneDay()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, new DateTime(2027, 1, 20));

        Assert.Equal(OfferStatus.Active, state.Status);
        Assert.Equal(1, state.DaysRemaining);
        Assert.Equal(0, state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_OnTheEndDate_IsEnded()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, new DateTime(2027, 1, 21));

        Assert.Equal(OfferStatus.Ended, state.Status);
        Assert.Equal(new DateTime(2027, 1, 21), state.EndDate);
        Assert.Null(state.DaysRemaining);
        Assert.Null(state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_AfterTheEnd_IsEnded()
    {
        Assert.Equal(OfferStatus.Ended, OfferPricing.Evaluate(ThreeMonths, new DateTime(2027, 2, 1)).Status);
    }

    [Fact]
    public void IsExpiringSoon_TrueWithinTheWarningWindow()
    {
        Assert.True(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2026, 12, 25)));
        Assert.True(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2026, 12, 22)));
    }

    [Fact]
    public void IsExpiringSoon_FalseOutsideTheWindowOrWhenEndedOrWithoutOffer()
    {
        Assert.False(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2026, 12, 21)));
        Assert.False(OfferPricing.IsExpiringSoon(ThreeMonths, Start));
        Assert.False(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2027, 1, 21)));
        Assert.False(OfferPricing.IsExpiringSoon(null, Start));
    }

    [Fact]
    public void ResolveStartDate_NewClientOnAPlanWithOffer_StartsToday()
    {
        Assert.Equal(Start, OfferPricing.ResolveStartDate(null, 5, true, null, Start));
    }

    [Fact]
    public void ResolveStartDate_ChangingToAnotherPlanWithOffer_RestartsToday()
    {
        var previous = new DateTime(2026, 1, 10);

        Assert.Equal(Start, OfferPricing.ResolveStartDate(3, 5, true, previous, Start));
    }

    [Fact]
    public void ResolveStartDate_SamePlan_KeepsTheCurrentStart()
    {
        var current = new DateTime(2026, 9, 1);

        Assert.Equal(current, OfferPricing.ResolveStartDate(5, 5, true, current, Start));
    }

    [Fact]
    public void ResolveStartDate_SamePlanThatGotAnOfferLater_IsNotRetroactive()
    {
        Assert.Null(OfferPricing.ResolveStartDate(5, 5, true, null, Start));
    }

    [Fact]
    public void ResolveStartDate_PlanWithoutOffer_ClearsTheStart()
    {
        Assert.Null(OfferPricing.ResolveStartDate(3, 5, false, new DateTime(2026, 9, 1), Start));
        Assert.Null(OfferPricing.ResolveStartDate(5, 5, false, new DateTime(2026, 9, 1), Start));
        Assert.Null(OfferPricing.ResolveStartDate(null, null, false, null, Start));
    }

    [Fact]
    public void ValidateOffer_NoOfferFields_IsValid()
    {
        Assert.Empty(OfferPricing.ValidateOffer(2500m, null, null));
    }

    [Fact]
    public void ValidateOffer_PriceBelowFeeWithMonths_IsValid()
    {
        Assert.Empty(OfferPricing.ValidateOffer(2500m, 1500m, 3));
        Assert.Empty(OfferPricing.ValidateOffer(2500m, 0m, 120));
    }

    [Fact]
    public void ValidateOffer_OnlyOneOfTheTwoFields_Fails()
    {
        Assert.Contains("La oferta requiere precio y meses de oferta.", OfferPricing.ValidateOffer(2500m, 1500m, null));
        Assert.Contains("La oferta requiere precio y meses de oferta.", OfferPricing.ValidateOffer(2500m, null, 3));
    }

    [Fact]
    public void ValidateOffer_MonthsOutOfRange_Fails()
    {
        Assert.Contains("Los meses de oferta deben estar entre 1 y 120.", OfferPricing.ValidateOffer(2500m, 1500m, 0));
        Assert.Contains("Los meses de oferta deben estar entre 1 y 120.", OfferPricing.ValidateOffer(2500m, 1500m, 121));
    }

    [Fact]
    public void ValidateOffer_PriceNotBelowTheFee_Fails()
    {
        Assert.Contains("El precio de oferta debe ser menor que la mensualidad.", OfferPricing.ValidateOffer(2500m, 2500m, 3));
        Assert.Contains("El precio de oferta debe ser menor que la mensualidad.", OfferPricing.ValidateOffer(2500m, 3000m, 3));
    }

    [Fact]
    public void ValidateOffer_NegativePrice_Fails()
    {
        Assert.Contains("El precio de oferta no puede ser negativo.", OfferPricing.ValidateOffer(2500m, -1m, 3));
    }

    [Fact]
    public void GetFullPriceUnit_DerivesTheFullPriceFromTheGross()
    {
        Assert.Equal(2500m, OfferPricing.GetFullPriceUnit(12000m, 6, 3, 1500m));
    }

    [Fact]
    public void GetFullPriceUnit_NullWhenEveryMonthWasOffer()
    {
        Assert.Null(OfferPricing.GetFullPriceUnit(4500m, 3, 3, 1500m));
    }
}
