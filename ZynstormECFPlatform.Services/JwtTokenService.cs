using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly AppSettings _appSettings;
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<AppSettings> appSettings, IOptions<JwtOptions>? jwtOptions = null)
    {
        _appSettings = appSettings.Value;
        _jwtOptions = jwtOptions?.Value ?? new JwtOptions();
    }

    private string EffectiveSecret => !string.IsNullOrEmpty(_jwtOptions.Secret) ? _jwtOptions.Secret : _appSettings.Secret;

    public ClaimsPrincipal GetPrincipalClaim(string token, string secret)
    {
        var secretToUse = !string.IsNullOrEmpty(secret) ? secret : EffectiveSecret;
        var key = Encoding.UTF8.GetBytes(secretToUse);
        var handler = new JwtSecurityTokenHandler();
        var validations = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = !string.IsNullOrEmpty(_jwtOptions.Issuer),
            ValidIssuer = !string.IsNullOrEmpty(_jwtOptions.Issuer) ? _jwtOptions.Issuer : null,
            ValidateAudience = !string.IsNullOrEmpty(_jwtOptions.Audience),
            ValidAudience = !string.IsNullOrEmpty(_jwtOptions.Audience) ? _jwtOptions.Audience : null,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        return handler.ValidateToken(token, validations, out _);
    }

    public int GetUserIdFromToken(string token, string secret)
    {
        var claims = GetPrincipalClaim(token, secret);
        var nameIdentifier = claims.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(nameIdentifier, out var parsedId) ? parsedId : 0;
    }

    public TokenDto CreateToken(User user, IdentityRole role)
    {
        var secret = Encoding.UTF8.GetBytes(EffectiveSecret);
        var issuedAt = DateTime.UtcNow;
        var minutes = _jwtOptions.AccessTokenMinutes > 0 ? _jwtOptions.AccessTokenMinutes : 1440;
        var expirationTime = issuedAt.AddMinutes(minutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role.Name!),
            }),
            Issuer = !string.IsNullOrEmpty(_jwtOptions.Issuer) ? _jwtOptions.Issuer : null,
            Audience = !string.IsNullOrEmpty(_jwtOptions.Audience) ? _jwtOptions.Audience : null,
            IssuedAt = issuedAt,
            Expires = expirationTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = ProccessCreateToken(tokenDescriptor);

        return new TokenDto
        {
            Token = token,
            Expiration = expirationTime
        };
    }

    private static string ProccessCreateToken(SecurityTokenDescriptor tokenDescriptor)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
