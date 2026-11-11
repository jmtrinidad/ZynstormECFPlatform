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

    [Fact]
    public void RegisterAcceptedInAccount_OnlyMarksDocumentsNotCountedYet()
    {
        Assert.Contains("\"BillingCountedAtUtc\" IS NULL", ClientUsageSql.RegisterAcceptedInAccount);
    }

    [Fact]
    public void RegisterAcceptedInAccount_UpsertsClientRowAndAccountRowFromTheSameMarkedDocument()
    {
        var sql = ClientUsageSql.RegisterAcceptedInAccount;

        Assert.Contains("INSERT INTO \"ClientMonthlyUsage\"", sql);
        Assert.Contains("ON CONFLICT (\"ClientId\", \"Year\", \"Month\")", sql);
        Assert.Contains("INSERT INTO \"BillingAccountMonthlyUsage\"", sql);
        Assert.Contains("ON CONFLICT (\"BillingAccountId\", \"Year\", \"Month\")", sql);
        Assert.Contains("\"AcceptedDocuments\" = \"BillingAccountMonthlyUsage\".\"AcceptedDocuments\" + 1", sql);
        Assert.Equal(2, CountOccurrences(sql, "FROM marked"));
    }

    [Fact]
    public void RegisterAcceptedInAccount_ClientRowNeverCarriesAFeeNorALimit()
    {
        // La fila del cliente es solo detalle: sin plan, sin mensualidad y con límite ilimitado (-1),
        // para que el cálculo de excedente por cliente nunca cobre nada desde ahí.
        Assert.Contains("CAST(NULL AS integer), @PlanName, 0, -1, @Now", ClientUsageSql.RegisterAcceptedInAccount);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
