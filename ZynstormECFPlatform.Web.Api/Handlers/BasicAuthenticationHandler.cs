using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Handlers;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppSettings _appSettings;
    private readonly ILogger _logger;
    private readonly IJwtTokenService _jwtTokenService;

    public BasicAuthenticationHandler(
         IJwtTokenService jwtTokenService,
         IOptions<AppSettings> appSettings,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TimeProvider clock)
        : base(options, logger, encoder)
    {
        _appSettings = appSettings.Value;
        _logger = logger.CreateLogger(typeof(BasicAuthenticationHandler));
        _jwtTokenService = jwtTokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var headers = Request.Headers.Authorization;

            if (!string.IsNullOrEmpty(headers))
            {
                var authHeader = AuthenticationHeaderValue.Parse(headers);

                switch (authHeader.Scheme)
                {
                    case "Basic":
                        var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter!)).Split(':');
                        var login = new UserLoginDto
                        {
                            UserName = credentials.FirstOrDefault()!,
                            Password = credentials.LastOrDefault()!
                        };

                        if (login.UserName.Equals("admin") && login.Password.Equals("admin"))
                        {
                            var claims = new[] {
                                new Claim( ClaimTypes.Name, login.UserName)
                                };

                            var identity = new ClaimsIdentity(claims, Scheme.Name);
                            var principal = new ClaimsPrincipal(identity);
                            var ticket = new AuthenticationTicket(principal, Scheme.Name);

                            return AuthenticateResult.Success(ticket);
                        }

                        throw new ArgumentException("Invalid credentials");

                    case "Bearer":
                        var claimsPrincipal = _jwtTokenService.GetPrincipalClaim(authHeader.Parameter!, _appSettings.Secret);
                        var ticke = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                        return AuthenticateResult.Success(ticke);

                    default:
                        break;
                }
            }
            return AuthenticateResult.NoResult();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, message: exception.Message);
            return AuthenticateResult.Fail($"Authentication failed");
        }
    }
}