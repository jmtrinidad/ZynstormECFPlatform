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

    public JwtTokenService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public ClaimsPrincipal GetPrincipalClaim(string token, string secret)
    {
        var key = Encoding.ASCII.GetBytes(secret);
        var handler = new JwtSecurityTokenHandler();
        var validations = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        return handler.ValidateToken(token, validations, out _);
    }

    public int GetUserIdFromToken(string token, string secret)
    {
        var key = Encoding.ASCII.GetBytes(secret);
        var handler = new JwtSecurityTokenHandler();
        var validations = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        var claims = handler.ValidateToken(token, validations, out _);
        var nameIdentifier = claims.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(nameIdentifier) ? 0 : int.Parse(nameIdentifier);
    }

    public TokenDto CreateToken(User user, IdentityRole role)
    {
        var secret = Encoding.UTF8.GetBytes(_appSettings.Secret);
        var issuedAt = DateTime.Now;
        var expirationTime = issuedAt.AddDays(1);

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