using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data.Seeds;
using ZynstormECFPlatform.Abstractions.Services;
using System.Text.Json;

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

    private readonly ICurrentUserService? _currentUserService;

    public StorageContext(
        DbContextOptions<StorageContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected StorageContext()
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        var userId = _currentUserService?.UserId;

        if (string.IsNullOrEmpty(userId)) return auditEntries;

        // Failsafe: verificar que el usuario realmente existe en la base de datos
        // para evitar excepciones de clave foránea FK_UserAuditLog_User en guardados
        if (!Users.Any(u => u.Id == userId)) return auditEntries;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is UserAuditLog || entry.Entity is UserAccessLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry);
            auditEntry.EntityName = entry.Entity.GetType().Name;
            auditEntry.UserId = userId;
            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.AuditType = "Create";
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.AuditType = "Delete";
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.AuditType = "Update";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        foreach (var auditEntry in auditEntries.Where(_ => !_.HasTemporaryProperties))
        {
            Set<UserAuditLog>().Add(auditEntry.ToAudit());
        }

        return auditEntries.Where(_ => _.HasTemporaryProperties).ToList();
    }

    private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            Set<UserAuditLog>().Add(auditEntry.ToAudit());
        }

        return base.SaveChangesAsync();
    }

    private class AuditEntry
    {
        public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            Entry = entry;
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
        public string UserId { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public Dictionary<string, object?> KeyValues { get; } = new();
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public string AuditType { get; set; } = null!;
        public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new();

        public bool HasTemporaryProperties => TemporaryProperties.Any();

        public UserAuditLog ToAudit()
        {
            var audit = new UserAuditLog();
            audit.UserId = UserId;
            audit.Action = AuditType;
            audit.EntityName = EntityName;
            audit.TimestampUtc = DateTime.UtcNow;
            audit.EntityId = JsonSerializer.Serialize(KeyValues);
            audit.PreviousState = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues);
            audit.NewState = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues);
            return audit;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);
        });

        modelBuilder.Entity<UserNotificationConfiguration>(entity =>
        {
            entity.HasKey(e => e.UserNotificationConfigurationId);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.NotificationTypeId).IsRequired();

            entity.Property(e => e.RegisteredAt)
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

            entity.HasOne(d => d.User)
                  .WithMany(p => p.UserNotificationConfigurations)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.NotificationType)
                  .WithMany(p => p.UserConfigurations)
                  .HasForeignKey(d => d.NotificationTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.NotificationTypeId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.Property(e => e.RegisteredAt)
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

            entity.HasData(
                new NotificationType { NotificationTypeId = 1, Name = "Factura Aceptada (Email)", Description = "Recibir email cuando una factura es aceptada por la DGII", RegisteredAt = DateTime.Parse("2026-05-01T00:00:00Z"), GuidId = "a1b2c3d4-e5f6-4a5b-9c8d-1e2f3a4b5c6d" },
                new NotificationType { NotificationTypeId = 2, Name = "Factura Rechazada (Email)", Description = "Recibir email cuando una factura es rechazada por la DGII", RegisteredAt = DateTime.Parse("2026-05-01T00:00:00Z"), GuidId = "b2c3d4e5-f6a1-4b6c-0d9e-2f3a4b5c6d7e" },
                new NotificationType { NotificationTypeId = 3, Name = "Reporte Diario", Description = "Recibir resumen diario de facturas procesadas", RegisteredAt = DateTime.Parse("2026-05-01T00:00:00Z"), GuidId = "c3d4e5f6-a1b2-4c7d-1e0f-3a4b5c6d7e8f" },
                new NotificationType { NotificationTypeId = 4, Name = "Reporte Semanal", Description = "Recibir resumen semanal con estadísticas detalladas", RegisteredAt = DateTime.Parse("2026-05-01T00:00:00Z"), GuidId = "d4e5f6a1-b2c3-4d8e-2f1a-4b5c6d7e8f9a" }
            );
        });

        modelBuilder.Entity<BusinessType>(entity =>
        {
            entity.HasKey(e => e.BusinessTypeId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.Property(e => e.RegisteredAt)
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

        modelBuilder.Entity<BusinessSimulationSample>(entity =>
        {
            entity.HasKey(e => e.BusinessSimulationSampleId);
            entity.Property(e => e.EcfType).IsRequired().HasMaxLength(2);
            entity.Property(e => e.JsonData).IsRequired();

            entity.Property(e => e.RegisteredAt)
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

            entity.HasOne(d => d.BusinessType)
                  .WithMany(p => p.Samples)
                  .HasForeignKey(d => d.BusinessTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

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

            entity.Property(e => e.DailyReportEmails)
                  .HasMaxLength(500)
                  .IsUnicode(false);

            entity.Property(e => e.WeeklyReportEmails)
                  .HasMaxLength(500)
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

            entity.HasData(
                new Currency { CurrencyId = 1, Code = "DOP", Name = "PESO DOMINICANO" },
                new Currency { CurrencyId = 2, Code = "USD", Name = "DOLAR ESTADOUNIDENSE" },
                new Currency { CurrencyId = 3, Code = "EUR", Name = "EURO" },
                new Currency { CurrencyId = 4, Code = "BRL", Name = "REAL BRASILENO" },
                new Currency { CurrencyId = 5, Code = "CAD", Name = "DOLAR CANADIENSE" },
                new Currency { CurrencyId = 6, Code = "CHF", Name = "FRANCO SUIZO" },
                new Currency { CurrencyId = 7, Code = "CHY", Name = "YUAN CHINO" },
                new Currency { CurrencyId = 8, Code = "XDR", Name = "DERECHO ESPECIAL DE GIRO" },
                new Currency { CurrencyId = 9, Code = "DKK", Name = "CORONA DANESA" },
                new Currency { CurrencyId = 10, Code = "GBP", Name = "LIBRA ESTERLINA" },
                new Currency { CurrencyId = 11, Code = "JPY", Name = "YEN JAPONES" },
                new Currency { CurrencyId = 12, Code = "NOK", Name = "CORONA NORUEGA" },
                new Currency { CurrencyId = 13, Code = "SCP", Name = "LIBRA ESCOCESA" },
                new Currency { CurrencyId = 14, Code = "SEK", Name = "CORONA SUECA" },
                new Currency { CurrencyId = 15, Code = "VEF", Name = "BOLIVAR FUERTE VENEZOLANO" },
                new Currency { CurrencyId = 16, Code = "HTG", Name = "GURDA HAITIANA" },
                new Currency { CurrencyId = 17, Code = "MXN", Name = "PESO MEXICANO" },
                new Currency { CurrencyId = 18, Code = "COP", Name = "PESO COLOMBIANO" }
            );
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

        modelBuilder.Entity<ReceivedB2BMessage>(entity =>
        {
            entity.HasKey(c => c.ReceivedB2BMessageId);

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(e => e.MessageType)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsUnicode(false);

            entity.Property(e => e.RncEmisor)
                  .HasMaxLength(25)
                  .IsUnicode(false);

            entity.Property(e => e.RncComprador)
                  .HasMaxLength(25)
                  .IsUnicode(false);

            entity.Property(e => e.ENcf)
                  .HasMaxLength(20)
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

            entity.HasOne(d => d.Client)
                  .WithMany(c => c.ReceivedB2BMessages)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_ReceivedB2BMessage_Client");
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

        modelBuilder.Entity<UserClient>(entity =>
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
                  .WithMany(p => p.UserClients)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_UserClient_Client");

            entity.HasOne(d => d.User)
                  .WithMany(p => p.UserClients)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_UserClient_User");
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

            entity.HasData(
                new CertificationStep { CertificationStepId = 1, Name = "Registrado", Order = 1, IsRequired = true },
                new CertificationStep { CertificationStepId = 2, Name = "Pruebas de Datos e-CF", Order = 2, IsRequired = true },
                new CertificationStep { CertificationStepId = 3, Name = "Pruebas de Datos Aprobación Comercial", Order = 3, IsRequired = true },
                new CertificationStep { CertificationStepId = 4, Name = "Pruebas Simulación e-CF", Order = 4, IsRequired = true },
                new CertificationStep { CertificationStepId = 5, Name = "Pruebas Simulación Representación Impresa", Order = 5, IsRequired = true },
                new CertificationStep { CertificationStepId = 6, Name = "Validación Representación Impresa", Order = 6, IsRequired = true },
                new CertificationStep { CertificationStepId = 7, Name = "URL Servicios Prueba", Order = 7, IsRequired = true },
                new CertificationStep { CertificationStepId = 8, Name = "Inicio Prueba Recepción e-CF", Order = 8, IsRequired = true },
                new CertificationStep { CertificationStepId = 9, Name = "Recepción e-CF", Order = 9, IsRequired = true },
                new CertificationStep { CertificationStepId = 10, Name = "Inicio Prueba Recepción Aprobación Comercial", Order = 10, IsRequired = true },
                new CertificationStep { CertificationStepId = 11, Name = "Recepción Aprobación Comercial", Order = 11, IsRequired = true },
                new CertificationStep { CertificationStepId = 12, Name = "URL Servicios Producción", Order = 12, IsRequired = true },
                new CertificationStep { CertificationStepId = 13, Name = "Declaración Jurada", Order = 13, IsRequired = true },
                new CertificationStep { CertificationStepId = 14, Name = "Verificación Estatus", Order = 14, IsRequired = true },
                new CertificationStep { CertificationStepId = 15, Name = "Finalizado", Order = 15, IsRequired = true }
            );
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

        modelBuilder.Entity<UserAccessLog>(entity =>
        {
            entity.HasKey(c => c.UserAccessLogId);
            entity.Property(e => e.RegisteredAt).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(e => e.AccessTimeUtc).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.GuidId).IsRequired().HasMaxLength(450).IsUnicode(false).HasDefaultValueSql(DefaultGUIDSqlValue);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_UserAccessLog_User");
        });

        modelBuilder.Entity<UserAuditLog>(entity =>
        {
            entity.HasKey(c => c.UserAuditLogId);
            entity.Property(e => e.RegisteredAt).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(e => e.TimestampUtc).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.GuidId).IsRequired().HasMaxLength(450).IsUnicode(false).HasDefaultValueSql(DefaultGUIDSqlValue);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_UserAuditLog_User");
        });

        BusinessSimulationSeeds.Seed(modelBuilder);
    }
}