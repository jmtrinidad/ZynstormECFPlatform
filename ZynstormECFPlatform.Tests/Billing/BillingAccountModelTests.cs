using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingAccountModelTests
{
    // Construir el modelo no abre conexión: la cadena solo selecciona el proveedor Npgsql.
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<StorageContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=x;Password=x")
            .Options;
        using var context = new StorageContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void BillingAccountMonthlyUsage_HasUnconditionalUniqueIndexOnAccountYearMonth()
    {
        var entity = BuildModel().FindEntityType(typeof(BillingAccountMonthlyUsage))!;

        var index = Assert.Single(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["BillingAccountId", "Year", "Month"]));

        Assert.True(index.IsUnique);
        Assert.Null(index.GetFilter());
        Assert.Equal("IX_BillingAccountMonthlyUsage_AccountId_Year_Month", index.GetDatabaseName());
    }

    [Fact]
    public void BillingAccount_AllowsOnlyOneActiveAccountPerOwner()
    {
        var entity = BuildModel().FindEntityType(typeof(BillingAccount))!;

        var index = Assert.Single(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["OwnerUserId"]));

        Assert.True(index.IsUnique);
        Assert.NotNull(index.GetFilter());
    }

    [Fact]
    public void Client_BillingAccountIdAndQuota_AreOptional()
    {
        var entity = BuildModel().FindEntityType(typeof(Client))!;

        Assert.True(entity.FindProperty(nameof(Client.BillingAccountId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(Client.QuotaDocuments))!.IsNullable);
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(BillingAccount)
            && fk.DeleteBehavior == DeleteBehavior.SetNull);
    }

    [Fact]
    public void BillingAccount_PaymentDefaultsMatchClient()
    {
        var entity = BuildModel().FindEntityType(typeof(BillingAccount))!;

        Assert.Equal(1, entity.FindProperty(nameof(BillingAccount.PaidMonths))!.GetDefaultValue());
        Assert.Equal(3, entity.FindProperty(nameof(BillingAccount.PaymentGraceDays))!.GetDefaultValue());
    }
}
