using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentReminderEmailsTests
{
    private static PaymentReminderEmailData Data(int planTypeId = (int)PlanTypeEnum.Rent, string name = "Cesar & Julio SRL") =>
        new(name, "BASICO EXP", planTypeId, 3600m, 3, new DateTime(2026, 9, 15), new DateTime(2026, 9, 18));

    [Fact]
    public void FirstReminder_MentionsDeadlineAmountAndCuts()
    {
        var (subject, html) = PaymentReminderEmails.BuildFirstReminder(Data());

        Assert.Contains("renta", subject);
        Assert.Contains("18/09/2026", html);
        Assert.Contains("RD$3,600.00", html);
        Assert.Contains("3 meses", html);
        Assert.Contains("cortes en el servicio y recargos", html);
    }

    [Fact]
    public void FirstReminder_EncodesClientName()
    {
        var (_, html) = PaymentReminderEmails.BuildFirstReminder(Data(name: "<b>Hack</b> & Co"));

        Assert.Contains("&lt;b&gt;Hack&lt;/b&gt; &amp; Co", html);
        Assert.DoesNotContain("<b>Hack</b>", html);
    }

    [Fact]
    public void DocumentPlan_TalksAboutMonthlyFee()
    {
        var (subject, html) = PaymentReminderEmails.BuildFirstReminder(Data((int)PlanTypeEnum.Documents));

        Assert.Contains("mensualidad", subject);
        Assert.DoesNotContain("renta", html);
    }

    [Fact]
    public void FinalReminder_SaysTodayIsTheDeadline()
    {
        var (subject, html) = PaymentReminderEmails.BuildFinalReminder(Data());

        Assert.Contains("Hoy vence el plazo", subject);
        Assert.Contains("hoy vence el plazo", html);
    }

    [Fact]
    public void Suspension_ExplainsReactivation()
    {
        var (subject, html) = PaymentReminderEmails.BuildSuspension(Data());

        Assert.Contains("suspendido", subject);
        Assert.Contains("se reactivará", html);
    }

    [Fact]
    public void AdminSummary_ListsClientsAndTotal()
    {
        PaymentSummaryItem[] items =
        [
            new("Cliente A", "101", "Renta 5", (int)PlanTypeEnum.Rent, new DateTime(2026, 9, 15), 1, 4000m, new DateTime(2026, 9, 18), "Primer aviso enviado hoy"),
            new("Cliente B", "202", "Pro", (int)PlanTypeEnum.Documents, new DateTime(2026, 9, 10), 6, 6300m, new DateTime(2026, 9, 13), "Suspendido")
        ];

        var (subject, html) = PaymentReminderEmails.BuildAdminSummary(items);

        Assert.Contains("2 cliente(s)", subject);
        Assert.Contains("Cliente A", html);
        Assert.Contains("Comprobantes", html);
        Assert.Contains("RD$10,300.00", html);
    }
}
