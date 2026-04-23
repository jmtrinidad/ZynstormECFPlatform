using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data.Seeds;

namespace ZynstormECFPlatform.Data;

public class StorageContext : IdentityDbContext<User, Role, string>, IStorageContext
{
    private string DefaultDateTimeSqlValue => Database.IsRelational() && Database.IsNpgsql() ? "CURRENT_TIMESTAMP" : "GETDATE()";

    private string DefaultGUIDSqlValue => Database.IsRelational() && Database.IsNpgsql() ? "gen_random_uuid()" : "NEWID()";

    //private string DefaultDueDateTimeSqlValue => Database.IsRelational() && Database.IsNpgsql() ? "CURRENT_TIMESTAMP + interval '1 month'" : "DATEADD(month, 1, GETDATE())";
    private string DateTimeColumnType => Database.IsRelational() && Database.IsNpgsql() ? "timestamp without time zone" : "datetime";

    //private string StringColumnType => Database.IsRelational() && Database.IsNpgsql() ? "text" : "NVARCHAR";

    public StorageContext(DbContextOptions<StorageContext> options) : base(options)
    {
    }

    public DbSet<CertificationProcess> CertificationProcesses { get; set; } = null!;
    public DbSet<CertificationStep> CertificationSteps { get; set; } = null!;
    public DbSet<CertificationDocument> CertificationDocuments { get; set; } = null!;
    public DbSet<ENcf> ENcfs { get; set; } = null!;

    protected StorageContext()
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global PostgreSQL DateTime column type configuration
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                modelBuilder.Entity(entityType.ClrType).Property(property.Name).HasColumnType(DateTimeColumnType);
            }
        }

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(c => c.ApiKeyId);

            entity.Property(e => e.Apikey)
                  .IsUnicode(false);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.SecretKey)
                  .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
               .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
               .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.ApiKeys)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ApiKey_Client");

            entity.HasOne(d => d.Status).WithMany(p => p.ApiKeys)
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ApiKey_Status");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(c => c.ClientId);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Name)
                  .HasMaxLength(50)
                  .IsUnicode(false);

            entity.Property(e => e.Phone)
                  .HasMaxLength(20)
                  .IsUnicode(false);

            entity.Property(e => e.Rnc)
                  .HasMaxLength(25)
                  .IsUnicode(false)
                  .HasColumnName("RNC");

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Status)
                  .WithMany(p => p.Clients)
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Client_Status");
        });

        modelBuilder.Entity<ClientBranche>(entity =>
        {
            entity.HasKey(c => c.ClientBrancheId);

            entity.Property(e => e.Address)
                  .HasMaxLength(200)
                  .IsUnicode(false);

            entity.Property(e => e.Code)
                  .HasMaxLength(20)
                  .IsUnicode(false);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.Email)
                  .HasMaxLength(30)
                  .IsUnicode(false);

            entity.Property(e => e.Name)
                  .HasMaxLength(80)
                  .IsUnicode(false);

            entity.Property(e => e.Phone)
                  .HasMaxLength(20)
                  .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.ClientBranches)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientBranche_Client");

            entity.HasOne(d => d.Status)
                  .WithMany(p => p.ClientBranches)
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientBranche_Status");
        });

        modelBuilder.Entity<ClientCallBack>(entity =>
        {
            entity.HasKey(c => c.ClientCallBackId);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.Secret)
                  .HasMaxLength(200)
                  .IsUnicode(false);

            entity.Property(e => e.Url)
                  .HasMaxLength(300)
                  .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Status)
                  .WithMany(p => p.ClientCallBacks)
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientCallBack_Status");

            entity.HasOne(d => d.ApiKey)
                  .WithMany(p => p.ClientCallBacks)
                  .HasForeignKey(d => d.ApiKeyId)
                  .HasConstraintName("FK_ClientCallBack_ApiKey");

            entity.HasOne(d => d.ClientBranche).WithMany(p => p.ClientCallBacks)
                .HasForeignKey(d => d.ClientBrancheId)
                .HasConstraintName("FK_ClientCallBack_ClientBranche");

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.ClientCallBacks)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientCallBack_Client");
        });

        modelBuilder.Entity<ClientCertificate>(entity =>
        {
            entity.HasKey(c => c.ClientCertificateId);

            entity.Property(e => e.Certificate).IsUnicode(false);
            entity.Property(e => e.RegisteredAt).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(e => e.ExpirationDateUtc).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.Password).IsUnicode(false);
            entity.Property(e => e.Thumbprint).IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.ClientCertificates)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientCertificate_Client");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(c => c.CurrencyId);

            entity.Property(e => e.Code)
                  .HasMaxLength(20)
                  .IsUnicode(false);

            entity.Property(e => e.Name)
                  .HasMaxLength(50)
                  .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<DGIIUnit>(entity =>
        {
            entity.HasKey(c => c.DGIIUnitId);

            entity.Property(e => e.DGIIUnitId);
            entity.Property(e => e.DGIICode);
            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<EcfDocument>(entity =>
        {
            entity.HasKey(c => c.EcfDocumentId);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.CustomerAddress)
                  .HasMaxLength(300)
                  .IsUnicode(false);

            entity.Property(e => e.CustomerEmail)
                  .HasMaxLength(200)
                  .IsUnicode(false);

            entity.Property(e => e.CustomerName)
                  .HasMaxLength(100)
                  .IsUnicode(false);

            entity.Property(e => e.CustomerRnc)
                  .HasMaxLength(50)
                  .IsUnicode(false);

            entity.Property(e => e.ExternalReference)
                .HasMaxLength(70)
                .IsUnicode(false);
            entity.Property(e => e.HangfireJobId)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.IssueDateUtc).HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.Itbistotal)
                  .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.Ncf)
                  .HasMaxLength(80)
                  .IsUnicode(false);

            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.ApiKey)
                  .WithMany(p => p.EcfDocuments)
                  .HasForeignKey(d => d.ApiKeyId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocument_ApiKey");

            entity.HasOne(d => d.ClientBranche)
                  .WithMany(p => p.EcfDocuments)
                  .HasForeignKey(d => d.ClientBrancheId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocument_ClientBranche");

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.EcfDocuments)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocument_Client");

            entity.HasOne(d => d.Currency)
                  .WithMany(p => p.EcfDocuments)
                  .HasForeignKey(d => d.CurrencyId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocument_Currency");

            entity.HasOne(d => d.EcfStatus)
                  .WithMany(p => p.EcfDocuments)
                  .HasForeignKey(d => d.EcfStatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocument_EcfStatus");

            entity.HasOne(d => d.EcfType)
                  .WithMany(p => p.EcfDocuments)
                  .HasForeignKey(d => d.EcfTypeId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocument_EcfType");

            // Extended Properties
            entity.Property(e => e.Version).HasMaxLength(10).IsUnicode(false).HasDefaultValue("1.0");
            entity.Property(e => e.SequenceExpirationDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.IncomeType).HasMaxLength(2).IsUnicode(false);
            entity.Property(e => e.PaymentDeadline).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.PaymentTerms).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.IssuerCommercialName).HasMaxLength(150).IsUnicode(false);
            entity.Property(e => e.IssuerBranchCode).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.IssuerMunicipality).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.IssuerProvince).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.IssuerActivityCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.IssuerSellerCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.IssuerWebSite).HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.IssuerAdditionalInfo).HasMaxLength(150).IsUnicode(false);
            entity.Property(e => e.IssuerPhone).HasMaxLength(12).IsUnicode(false);
            entity.Property(e => e.CustomerContact).HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.CustomerMunicipality).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.CustomerProvince).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.CustomerTelephone).HasMaxLength(12).IsUnicode(false);
            entity.Property(e => e.DeliveryDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.DeliveryContact).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.AdditionalPhone).HasMaxLength(12).IsUnicode(false);
            entity.Property(e => e.PurchaseOrderDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.PurchaseOrderNumber).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.ModifiedNcf).HasMaxLength(19).IsUnicode(false);
            entity.Property(e => e.ModifiedNcfDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.ModificationReason).HasMaxLength(90).IsUnicode(false);
            entity.Property(e => e.SignatureDateTime).HasColumnType(DateTimeColumnType);

            // Foreign customer fields (Types 46/47)
            entity.Property(e => e.CustomerForeignId).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.CustomerCountry).HasMaxLength(60).IsUnicode(false);

            // Issuer email
            entity.Property(e => e.IssuerEmail).HasMaxLength(80).IsUnicode(false);

            // Reference customer RNC (NC/ND against a different taxpayer)
            entity.Property(e => e.ReferenceCustomerRnc).HasMaxLength(25).IsUnicode(false);
        });

        modelBuilder.Entity<EcfDocumentDetail>(entity =>
        {
            entity.HasKey(c => c.EcfDocumentDetailId);

            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ItbisAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ItbisPercentage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfDocumentDetails)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocumentDetail_EcfDocument");

            // Extended Properties
            entity.Property(e => e.ItemName).HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.WithholdingItbis).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.WithholdingIsr).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ItemAmount).HasColumnType("decimal(18, 2)");

            // ISC fields
            entity.Property(e => e.IscType).HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.AdditionalTaxRate).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IscSpecificAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IscAdvaloremAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OtherAdditionalTaxAmount).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<EcfDocumentTotal>(entity =>
        {
            entity.HasKey(c => c.EcfDocumentId);

            entity.Property(e => e.DiscountTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExemptTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ITBISTotal)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxableTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfDocumentTotals)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocumentTotal_EcfDocument");

            // Extended Properties
            entity.Property(e => e.TaxableAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxableAmountG1).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxableAmountG2).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxableAmountG3).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount1).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount2).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount3).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWithheldItbis).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalWithheldIsr).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AdditionalTaxTotal).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<EcfDocumentAdditionalTax>(entity =>
        {
            entity.HasKey(c => c.EcfDocumentAdditionalTaxId);

            entity.Property(e => e.TaxTypeCode)
                  .HasMaxLength(3)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.TaxRate).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IscSpecificAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IscAdvaloremAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OtherAdditionalTaxAmount).HasColumnType("decimal(18, 2)");

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfDocument)
                  .WithMany(p => p.EcfDocumentAdditionalTaxes)
                  .HasForeignKey(d => d.EcfDocumentId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_EcfDocumentAdditionalTax_EcfDocument");
        });

        modelBuilder.Entity<EcfStatus>(entity =>
        {
            entity.HasKey(c => c.EcfStatusId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasData(
                new EcfStatus { EcfStatusId = 1, Name = "Created" },
                new EcfStatus { EcfStatusId = 2, Name = "Validating" },
                new EcfStatus { EcfStatusId = 3, Name = "ValidationFailed" },
                new EcfStatus { EcfStatusId = 4, Name = "ReadyToSign" },
                new EcfStatus { EcfStatusId = 5, Name = "Signing" },
                new EcfStatus { EcfStatusId = 6, Name = "Signed" },
                new EcfStatus { EcfStatusId = 7, Name = "SendPending" },
                new EcfStatus { EcfStatusId = 8, Name = "Sending" },
                new EcfStatus { EcfStatusId = 9, Name = "Sent" },
                new EcfStatus { EcfStatusId = 10, Name = "Accepted" },
                new EcfStatus { EcfStatusId = 11, Name = "Rejected" },
                new EcfStatus { EcfStatusId = 12, Name = "Error" },
                new EcfStatus { EcfStatusId = 13, Name = "Cancelled" }
                );
        });

        modelBuilder.Entity<EcfStatusHistory>(entity =>
        {
            entity.HasKey(c => c.EcfStatusHistoryId);

            entity.Property(e => e.RegisteredAt)
                .HasColumnType(DateTimeColumnType)
                .HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(e => e.Message).HasColumnType("text");

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfStatusHistories)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfStatusHistory_EcfDocument");

            entity.HasOne(d => d.EcfStatus).WithMany(p => p.EcfStatusHistories)
                .HasForeignKey(d => d.EcfStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfStatusHistory_EcfStatus");
        });

        modelBuilder.Entity<EcfTransmission>(entity =>
        {
            entity.HasKey(c => c.EcfTransmissionId);

            entity.Property(e => e.RequestPayload).HasColumnType("text");
            entity.Property(e => e.ResponseCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.ResponseMessage).HasColumnType("text");
            entity.Property(e => e.ResponsePayload).HasColumnType("text");
            entity.Property(e => e.SentAtUtc).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(e => e.TrackId)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfTransmissions)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfTransmission_EcfDocument");

            entity.HasOne(d => d.EcfStatus).WithMany(p => p.EcfTransmissions)
                .HasForeignKey(d => d.EcfStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfTransmission_EcfStatus");
        });

        modelBuilder.Entity<IncomingEcfDocument>(entity =>
        {
            entity.HasKey(c => c.IncomingEcfDocumentId);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.RncEmisor)
                  .HasMaxLength(25)
                  .IsUnicode(false);

            entity.Property(e => e.ENcf)
                  .HasMaxLength(20)
                  .IsUnicode(false);

            entity.Property(e => e.TrackId)
                  .HasMaxLength(100)
                  .IsUnicode(false);

            entity.Property(e => e.RawXml)
                  .HasColumnType("text");

            entity.Property(e => e.ReceivedAtUtc)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<EcfType>(entity =>
        {
            entity.HasKey(c => c.EcfTypeId);

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.Property(e => e.Name)
                  .HasMaxLength(50)
                  .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)

                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasData(
                new EcfType { EcfTypeId = 1, Code = "31", Name = "Factura de Crédito Fiscal Electrónica" },
                new EcfType { EcfTypeId = 2, Code = "32", Name = "Factura de Consumo Electrónica" },
                new EcfType { EcfTypeId = 3, Code = "33", Name = "Nota de Débito Electrónica" },
                new EcfType { EcfTypeId = 4, Code = "34", Name = "Nota de Crédito Electrónica" },
                new EcfType { EcfTypeId = 5, Code = "41", Name = "Compras Electrónico" },
                new EcfType { EcfTypeId = 6, Code = "43", Name = "Gastos Menores Electrónico" },
                new EcfType { EcfTypeId = 7, Code = "44", Name = "Regímenes Especiales Electrónico" },
                new EcfType { EcfTypeId = 8, Code = "45", Name = "Gubernamental Electrónico" },
                new EcfType { EcfTypeId = 9, Code = "46", Name = "Comprobante de Exportaciones Electrónico" },
                new EcfType { EcfTypeId = 10, Code = "47", Name = "Comprobante para Pagos al Exterior Electrónico" }
                );
        });

        modelBuilder.Entity<EcfXmlDocument>(entity =>
        {
            entity.HasKey(c => c.EcfXmlDocumentId);

            entity.Property(e => e.RegisteredAt)
                .HasColumnType(DateTimeColumnType)
                .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.XmlSigned).HasColumnType("text");

            entity.Property(e => e.XmlUnsigned).HasColumnType("text");

            entity.Property(c => c.LastUpdateUtc)
               .HasColumnType(DateTimeColumnType)
               .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)

                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfDocument)
                  .WithMany(p => p.EcfXmlDocuments)
                 .HasForeignKey(d => d.EcfDocumentId)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_EcfXmlDocument_EcfDocument");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(c => c.StatusId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)

                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasData(
                new Status { StatusId = 1, Name = "Active" },
                new Status { StatusId = 2, Name = "Inactive" },
                new Status { StatusId = 3, Name = "Suspended" },
                new Status { StatusId = 4, Name = "Deleted" }
                );
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(c => c.SystemLogId);

            entity.Property(e => e.CreateAtUtc).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(e => e.Exception).HasColumnType("text");
            entity.Property(e => e.LogLevel)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.SystemLogId).ValueGeneratedOnAdd();

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)

                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                 .WithMany(c => c.SystemLogs)
                 .HasForeignKey(d => d.ClientId)
                 .OnDelete(DeleteBehavior.ClientSetNull)
                 .HasConstraintName("FK_SystemLog_Client");

            entity.HasOne(d => d.EcfDocument)
                  .WithMany(c => c.SystemLogs)
                  .HasForeignKey(d => d.EcfDocumentId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_SystemLog_EcfDocument");
        });

        modelBuilder.Entity<UseClient>(entity =>
        {
            entity.HasKey(c => new { c.UserId, c.ClientId });

            entity.Property(e => e.UserId)
                       .IsRequired()
                       .HasMaxLength(450)
                       .IsUnicode(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.UseClients)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_UseClient_Client");

            entity.HasOne(d => d.User)
                  .WithMany(p => p.UseClients)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_UseClient_User");
        });

        modelBuilder.Entity<DgiiMunicipality>(entity =>
        {
            entity.HasKey(c => c.DgiiMunicipalityId);

            entity.Property(e => e.Code)
                  .IsRequired()
                  .HasMaxLength(6)
                  .IsFixedLength()
                  .IsUnicode(false);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(150)
                  .IsUnicode(false);

            entity.Property(e => e.IsProvince)
                  .HasDefaultValue(false);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            DgiiMunicipalitySeeds.Seed(entity);
        });

        modelBuilder.Entity<CertificationStep>(entity =>
        {
            entity.ToTable("CertificationStep");
            entity.HasKey(c => c.CertificationStepId);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.Order)
                  .IsRequired();

            entity.Property(e => e.IsRequired)
                  .HasDefaultValue(true);

            entity.Property(c => c.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<CertificationProcess>(entity =>
        {
            entity.ToTable("CertificationProcess");
            entity.HasKey(c => c.CertificationProcessId);

            entity.Property(e => e.Environment)
                  .IsRequired();

            entity.Property(e => e.Status)
                  .IsRequired();

            entity.Property(e => e.StartDate)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.EndDate)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.CertificationProcesses)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationProcess_Client");

            entity.HasOne(d => d.CertificationStep)
                  .WithMany(p => p.CertificationProcesses)
                  .HasForeignKey(d => d.CurrentStepId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationProcess_CertificationStep");
        });

        modelBuilder.Entity<CertificationDocument>(entity =>
        {
            entity.ToTable("CertificationDocument");
            entity.HasKey(c => c.CertificationDocumentId);

            entity.Property(e => e.ENcfSecuence)
                  .HasMaxLength(20)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.XmlSent)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.XmlResponse)
                  .IsUnicode(false);

            entity.Property(e => e.TrackId)
                  .HasMaxLength(100)
                  .IsUnicode(false);

            entity.Property(e => e.Status)
                  .IsRequired();

            entity.Property(e => e.SentAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.ValidatedAt)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.CertificationProcess)
                  .WithMany(p => p.CertificationDocuments)
                  .HasForeignKey(d => d.CertificationProcessId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationDocument_CertificationProcess");

            entity.HasOne(d => d.EcfType)
                  .WithMany(p => p.CertificationDocuments)
                  .HasForeignKey(d => d.EcfTypeId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationDocument_EcfType");

            entity.HasOne(d => d.ENcf)
                  .WithMany(p => p.CertificationDocuments)
                  .HasForeignKey(d => d.ENcfId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationDocument_ENcf");
        });

        modelBuilder.Entity<CertificationInvoicePrintTemplate>(entity =>
        {
            entity.HasKey(c => c.CertificationInvoicePrintTemplateId);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(250)
                  .IsUnicode(false);

            entity.Property(e => e.FileUrl)
                  .HasMaxLength(500)
                  .IsUnicode(false);

            entity.Property(e => e.FileName)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.ContentType)
                  .HasMaxLength(50)
                  .IsUnicode(false)
                  .HasDefaultValue("application/pdf")
                  .IsRequired();

            entity.Property(c => c.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.CertificationInvoicePrintTemplates)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationInvoicePrintTemplate_Client");

            entity.HasOne(d => d.EcfType)
                  .WithMany(p => p.CertificationInvoicePrintTemplates)
                  .HasForeignKey(d => d.EcfTypeId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_CertificationInvoicePrintTemplate_EcfType");
        });

        modelBuilder.Entity<ENcf>(entity =>
        {
            entity.ToTable("ENcf");
            entity.HasKey(c => c.ENcfId);

            entity.Property(e => e.Sequence)
                  .IsRequired();

            entity.Property(c => c.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.EcfType)
                  .WithMany(p => p.ENcfs)
                  .HasForeignKey(d => d.NcfTypeId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ENcf_EcfType");

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.ENcfs)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ENcf_Client");
        });
    }
}