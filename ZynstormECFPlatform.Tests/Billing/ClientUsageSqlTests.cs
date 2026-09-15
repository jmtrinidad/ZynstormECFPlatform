using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientUsageSqlTests
{
    [Fact]
    public void RegisterAccepted_OnlyMarksDocumentsNotCountedYet()
    {
        Assert.Contains("\"BillingCountedAtUtc\" IS NULL", ClientUsageSql.RegisterAccepted);
    }

    [Fact]
    public void RegisterAccepted_UpsertsOnClientYearMonth()
    {
        Assert.Contains("ON CONFLICT (\"ClientId\", \"Year\", \"Month\")", ClientUsageSql.RegisterAccepted);
        Assert.Contains("\"AcceptedDocuments\" = \"ClientMonthlyUsage\".\"AcceptedDocuments\" + 1", ClientUsageSql.RegisterAccepted);
    }

    [Fact]
    public void RegisterAccepted_InsertsOnlyWhenDocumentWasMarked()
    {
        Assert.Contains("FROM marked", ClientUsageSql.RegisterAccepted);
    }
}
