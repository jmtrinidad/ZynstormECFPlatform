using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentReminderPolicyTests
{
    private static readonly DateTime Due = new(2026, 9, 15);

    private static PaymentReminderAction Decide(
        DateTime today,
        int graceDays = 3,
        DateTime? firstSentFor = null,
        DateTime? finalSentFor = null,
        bool hasEmail = true,
        bool isSuspended = false,
        DateTime? due = null) =>
        PaymentReminderPolicy.Decide(due ?? Due, graceDays, firstSentFor, finalSentFor, hasEmail, isSuspended, today);

    [Fact]
    public void WithoutNextPaymentDate_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None,
            PaymentReminderPolicy.Decide(null, 3, null, null, true, false, new DateTime(2026, 9, 20)));
    }

    [Fact]
    public void BeforeDueDate_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None, Decide(new DateTime(2026, 9, 10)));
    }

    [Fact]
    public void OnDueDate_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None, Decide(Due));
    }

    [Fact]
    public void OneDayOverdue_SendsFirstReminder()
    {
        Assert.Equal(PaymentReminderAction.FirstReminder, Decide(Due.AddDays(1)));
    }

    [Fact]
    public void OneDayOverdue_FirstAlreadySent_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None, Decide(Due.AddDays(1), firstSentFor: Due));
    }

    [Fact]
    public void TwoDaysOverdue_FirstAlreadySent_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None, Decide(Due.AddDays(2), firstSentFor: Due));
    }

    [Fact]
    public void TwoDaysOverdue_FirstNeverSent_CatchesUpWithFirstReminder()
    {
        Assert.Equal(PaymentReminderAction.FirstReminder, Decide(Due.AddDays(2)));
    }

    [Fact]
    public void GraceDayReached_SendsFinalReminder()
    {
        Assert.Equal(PaymentReminderAction.FinalReminder, Decide(Due.AddDays(3), firstSentFor: Due));
    }

    [Fact]
    public void GraceDayReached_FinalAlreadySent_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None, Decide(Due.AddDays(3), firstSentFor: Due, finalSentFor: Due));
    }

    [Fact]
    public void DayAfterGrace_FinalSentBefore_Suspends()
    {
        Assert.Equal(PaymentReminderAction.Suspend, Decide(Due.AddDays(4), firstSentFor: Due, finalSentFor: Due));
    }

    [Fact]
    public void DayAfterGrace_FinalNeverSent_SendsFinalFirst()
    {
        // El cliente siempre recibe el último aviso antes del corte.
        Assert.Equal(PaymentReminderAction.FinalReminder, Decide(Due.AddDays(4), firstSentFor: Due));
    }

    [Fact]
    public void DayAfterGrace_WithoutEmail_Suspends()
    {
        Assert.Equal(PaymentReminderAction.Suspend, Decide(Due.AddDays(4), hasEmail: false));
    }

    [Fact]
    public void AlreadySuspended_DoesNothing()
    {
        Assert.Equal(PaymentReminderAction.None,
            Decide(Due.AddDays(10), firstSentFor: Due, finalSentFor: Due, isSuspended: true));
    }

    [Fact]
    public void MarksFromPreviousCycle_AreIgnored()
    {
        var previousDue = Due.AddMonths(-1);

        Assert.Equal(PaymentReminderAction.FirstReminder,
            Decide(Due.AddDays(1), firstSentFor: previousDue, finalSentFor: previousDue));
    }

    [Fact]
    public void GraceOfOneDay_FirstOverdueDayIsTheFinalReminder()
    {
        Assert.Equal(PaymentReminderAction.FinalReminder, Decide(Due.AddDays(1), graceDays: 1));
    }

    [Fact]
    public void CustomGrace_UsesClientValue()
    {
        Assert.Equal(PaymentReminderAction.None, Decide(Due.AddDays(3), graceDays: 5, firstSentFor: Due));
        Assert.Equal(PaymentReminderAction.FinalReminder, Decide(Due.AddDays(5), graceDays: 5, firstSentFor: Due));
    }

    [Fact]
    public void InvalidGrace_FallsBackToDefault()
    {
        Assert.Equal(PaymentReminderAction.FinalReminder, Decide(Due.AddDays(3), graceDays: 0, firstSentFor: Due));
    }

    [Fact]
    public void DatesWithTime_AreComparedAsCalendarDays()
    {
        Assert.Equal(PaymentReminderAction.FirstReminder,
            Decide(new DateTime(2026, 9, 16, 8, 0, 0), due: new DateTime(2026, 9, 15, 23, 0, 0)));
    }

    [Fact]
    public void GetDeadline_AddsGraceDays()
    {
        Assert.Equal(new DateTime(2026, 9, 18), PaymentReminderPolicy.GetDeadline(Due, 3));
    }

    [Fact]
    public void GetDaysOverdue_IsNegativeBeforeDueDate()
    {
        Assert.Equal(-5, PaymentReminderPolicy.GetDaysOverdue(Due, new DateTime(2026, 9, 10)));
        Assert.Equal(2, PaymentReminderPolicy.GetDaysOverdue(Due, new DateTime(2026, 9, 17)));
    }
}
