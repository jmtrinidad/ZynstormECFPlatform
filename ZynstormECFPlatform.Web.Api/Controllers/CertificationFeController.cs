using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using Microsoft.AspNetCore.Hosting;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/certification/fe")]
[AllowAnonymous]
[ApiController]
public class CertificationFeController : FeController
{
    public CertificationFeController(
        ICacheService cacheService,
        IJwtTokenService jwtTokenService,
        IInboundEcfService inboundEcfService,
        ILogger<FeController> logger,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IClientCertificateService clientCertificateService,
        IEncryptedService encryptedService,
        IDgiiAuthService dgiiAuthService,
        IReceivedB2BMessageService receivedB2BMessageService,
        IEmailService emailService)
        : base(
            cacheService,
            jwtTokenService,
            inboundEcfService,
            logger,
            clientService,
            apiKeyService,
            clientCertificateService,
            encryptedService,
            dgiiAuthService,
            receivedB2BMessageService,
            emailService)
    {
    }
}
