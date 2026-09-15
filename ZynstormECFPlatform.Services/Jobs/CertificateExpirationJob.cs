using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Certificates;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Revisa diariamente el certificado vigente de cada cliente activo y envía un resumen
/// al correo administrativo con los que están vencidos o por vencer.
/// </summary>
public class CertificateExpirationJob
{
    private readonly IRepository<ClientCertificate> _certificateRepository;
    private readonly IEmailService _emailService;
    private readonly AppSettings _settings;
    private readonly ILogger<CertificateExpirationJob> _logger;

    public CertificateExpirationJob(
        IRepository<ClientCertificate> certificateRepository,
        IEmailService emailService,
        IOptions<AppSettings> options,
        ILogger<CertificateExpirationJob> logger)
    {
        _certificateRepository = certificateRepository;
        _emailService = emailService;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task CheckExpiringCertificatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Certificate Expiration Job...");

        var warningDays = _settings.CertificateExpirationWarningDays > 0
            ? _settings.CertificateExpirationWarningDays
            : CertificateExpirationHelper.DefaultWarningDays;

        var certificates = await _certificateRepository.Table
            .AsNoTracking()
            .Include(c => c.Client)
            .Where(c => !c.Client.ClientInactive && !c.Client.IsDeleted)
            .ToListAsync(cancellationToken);

        var expiring = certificates
            .GroupBy(c => c.ClientId)
            .Select(g => CertificateExpirationHelper.SelectActive(g))
            .Where(c => c?.ExpirationDateUtc != null)
            .Select(c => new ExpiringCertificateInfo(
                c!.Client.Name,
                c.Client.Rnc,
                c.Client.Email,
                c.ExpirationDateUtc!.Value,
                CertificateExpirationHelper.GetDaysToExpire(c.ExpirationDateUtc)!.Value))
            .Where(x => x.DaysToExpire <= warningDays)
            .OrderBy(x => x.DaysToExpire)
            .ToList();

        if (expiring.Count == 0)
        {
            _logger.LogInformation("Certificate Expiration Job: no certificates expiring within {Days} days.", warningDays);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.CertificateAlertEmail))
        {
            _logger.LogWarning("Certificate Expiration Job: {Count} certificates expiring but AppSettings:CertificateAlertEmail is not configured.", expiring.Count);
            return;
        }

        var (subject, htmlBody) = CertificateExpirationHelper.BuildAdminSummaryEmail(expiring, warningDays);
        await _emailService.SendEmailAsync(_settings.CertificateAlertEmail, subject, htmlBody, cancellationToken: cancellationToken);

        _logger.LogInformation("Certificate Expiration Job: notified {Count} expiring certificates to {Email}.", expiring.Count, _settings.CertificateAlertEmail);
    }
}
