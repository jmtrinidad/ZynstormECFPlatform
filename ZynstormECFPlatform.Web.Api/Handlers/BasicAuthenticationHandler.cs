using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Abstractions.DataServices;
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
                        return await AuthenticateWithJwtAsync(authHeader.Parameter!).ConfigureAwait(false);

                    default:
                        break;
                }
            }

            // Sin header Authorization: el JWT puede venir en la cookie httpOnly.
            // Este handler es el esquema DEFAULT en Development, así que necesita el mismo
            // fallback que el middleware JwtBearer estándar.
            var cookieToken = Helpers.AuthCookie.ReadToken(Request);
            if (!string.IsNullOrEmpty(cookieToken))
            {
                return await AuthenticateWithJwtAsync(cookieToken).ConfigureAwait(false);
            }

            return AuthenticateResult.NoResult();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, message: exception.Message);
            return AuthenticateResult.Fail($"Authentication failed");
        }
    }

    private async Task<AuthenticateResult> AuthenticateWithJwtAsync(string token)
    {
        var claimsPrincipal = _jwtTokenService.GetPrincipalClaim(token, _appSettings.Secret);

        var nameIdentifier = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(nameIdentifier))
        {
            var accountService = Context.RequestServices.GetRequiredService<IAccountService>();
            var user = await accountService.GetUserByIdAsync(nameIdentifier).ConfigureAwait(false);
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                return AuthenticateResult.Fail("User does not exist or is inactive");
            }
        }

        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}