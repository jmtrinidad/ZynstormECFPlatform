using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ZynstormEcfPlatformContext : DbContext
{
    public ZynstormEcfPlatformContext()
    {
    }

    public ZynstormEcfPlatformContext(DbContextOptions<ZynstormEcfPlatformContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiKey> ApiKeys { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientBranche> ClientBranches { get; set; }

    public virtual DbSet<ClientCallBack> ClientCallBacks { get; set; }

    public virtual DbSet<ClientCertificate> ClientCertificates { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<Dgiiunit> Dgiiunits { get; set; }

    public virtual DbSet<EcfDocument> EcfDocuments { get; set; }

    public virtual DbSet<EcfDocumentDetail> EcfDocumentDetails { get; set; }

    public virtual DbSet<EcfDocumentTotal> EcfDocumentTotals { get; set; }

    public virtual DbSet<EcfStatus> EcfStatuses { get; set; }

    public virtual DbSet<EcfStatusHistory> EcfStatusHistories { get; set; }

    public virtual DbSet<EcfTransmission> EcfTransmissions { get; set; }

    public virtual DbSet<EcfType> EcfTypes { get; set; }

    public virtual DbSet<EcfXmlDocument> EcfXmlDocuments { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<UseClient> UseClients { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=217.216.91.10,15432;Database=zynstorm_ecf_platform;User Id=sa;Password=rEMIGIO13579@#;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKey");

            entity.Property(e => e.Apikey)
                .IsUnicode(false)
                .HasColumnName("APIKey");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.SecretKey).IsUnicode(false);

            entity.HasOne(d => d.Client).WithMany(p => p.ApiKeys)
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
            entity.ToTable("Client");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
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

            entity.HasOne(d => d.Status).WithMany(p => p.Clients)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Client_Status");
        });

        modelBuilder.Entity<ClientBranche>(entity =>
        {
            entity.ToTable("ClientBranche");

            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Client).WithMany(p => p.ClientBranches)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientBranche_Client");

            entity.HasOne(d => d.Status).WithMany(p => p.ClientBranches)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientBranche_Status");
        });

        modelBuilder.Entity<ClientCallBack>(entity =>
        {
            entity.ToTable("ClientCallBack");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Secret)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Url)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.ApiKey).WithMany(p => p.ClientCallBacks)
                .HasForeignKey(d => d.ApiKeyId)
                .HasConstraintName("FK_ClientCallBack_ApiKey");

            entity.HasOne(d => d.ClientBranche).WithMany(p => p.ClientCallBacks)
                .HasForeignKey(d => d.ClientBrancheId)
                .HasConstraintName("FK_ClientCallBack_ClientBranche");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientCallBacks)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientCallBack_Client1");
        });

        modelBuilder.Entity<ClientCertificate>(entity =>
        {
            entity.ToTable("ClientCertificate");

            entity.Property(e => e.Certificate).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpirationDate).HasColumnType("datetime");
            entity.Property(e => e.Password).IsUnicode(false);
            entity.Property(e => e.Thumbprint).IsUnicode(false);

            entity.HasOne(d => d.Client).WithMany(p => p.ClientCertificates)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientCertificate_Client");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("Currency");

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Dgiiunit>(entity =>
        {
            entity.ToTable("DGIIUnit");

            entity.Property(e => e.DgiiunitId).HasColumnName("DGIIUnitId");
            entity.Property(e => e.Dgiicode).HasColumnName("DGIICode");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EcfDocument>(entity =>
        {
            entity.ToTable("EcfDocument");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
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
                .IsUnicode(false)
                .HasColumnName("CustomerRNC");
            entity.Property(e => e.ExternalReference)
                .HasMaxLength(70)
                .IsUnicode(false);
            entity.Property(e => e.HangfireJobId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IssueDate).HasColumnType("datetime");
            entity.Property(e => e.Itbistotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ITBISTotal");
            entity.Property(e => e.Ncf)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("NCF");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ApiKey).WithMany(p => p.EcfDocuments)
                .HasForeignKey(d => d.ApiKeyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocument_ApiKey");

            entity.HasOne(d => d.ClientBranche).WithMany(p => p.EcfDocuments)
                .HasForeignKey(d => d.ClientBrancheId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocument_ClientBranche");

            entity.HasOne(d => d.Client).WithMany(p => p.EcfDocuments)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocument_Client");

            entity.HasOne(d => d.Currency).WithMany(p => p.EcfDocuments)
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocument_Currency");

            entity.HasOne(d => d.EcfStatus).WithMany(p => p.EcfDocuments)
                .HasForeignKey(d => d.EcfStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocument_EcfStatus");

            entity.HasOne(d => d.EcfType).WithMany(p => p.EcfDocuments)
                .HasForeignKey(d => d.EcfTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocument_EcfType");
        });

        modelBuilder.Entity<EcfDocumentDetail>(entity =>
        {
            entity.ToTable("EcfDocumentDetail");

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

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfDocumentDetails)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocumentDetail_EcfDocument");
        });

        modelBuilder.Entity<EcfDocumentTotal>(entity =>
        {
            entity.ToTable("EcfDocumentTotal");

            entity.Property(e => e.DiscountTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExemptTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Itbistotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ITBISTotal");
            entity.Property(e => e.TaxableTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfDocumentTotals)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfDocumentTotal_EcfDocument");
        });

        modelBuilder.Entity<EcfStatus>(entity =>
        {
            entity.ToTable("EcfStatus");

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EcfStatusHistory>(entity =>
        {
            entity.ToTable("EcfStatusHistory");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Message).HasColumnType("text");

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
            entity.ToTable("EcfTransmission");

            entity.Property(e => e.RequestPayload).HasColumnType("text");
            entity.Property(e => e.ResponseCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ResponseMessage).HasColumnType("text");
            entity.Property(e => e.ResponsePayload).HasColumnType("text");
            entity.Property(e => e.SentAt).HasColumnType("datetime");
            entity.Property(e => e.TrackId)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfTransmissions)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfTransmission_EcfDocument");

            entity.HasOne(d => d.EcfStatus).WithMany(p => p.EcfTransmissions)
                .HasForeignKey(d => d.EcfStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfTransmission_EcfStatus");
        });

        modelBuilder.Entity<EcfType>(entity =>
        {
            entity.ToTable("EcfType");

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EcfXmlDocument>(entity =>
        {
            entity.ToTable("EcfXmlDocument");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.XmlSigned).HasColumnType("text");
            entity.Property(e => e.XmlUnsigned).HasColumnType("text");

            entity.HasOne(d => d.EcfDocument).WithMany(p => p.EcfXmlDocuments)
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EcfXmlDocument_EcfDocument");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SystemLog");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Exception).HasColumnType("text");
            entity.Property(e => e.LogLevel)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.SystemLogId).ValueGeneratedOnAdd();

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemLog_Client");

            entity.HasOne(d => d.EcfDocument).WithMany()
                .HasForeignKey(d => d.EcfDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemLog_EcfDocument");
        });

        modelBuilder.Entity<UseClient>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UseClient");

            entity.Property(e => e.UserId).IsUnicode(false);

            entity.HasOne(d => d.Client).WithMany()
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UseClient_Client");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
