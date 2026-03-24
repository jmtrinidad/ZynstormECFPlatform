using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services;

public class JwtTokenService : IJwtTokenService
{
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
        return int.Parse(claims.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value);
    }

    public (string token, DateTime exp) CreateToken(User user, string secret, Role rol)
    {
        var key = Encoding.ASCII.GetBytes(secret);
        var issuedAt = DateTime.UtcNow;
        var expirationTime = issuedAt.AddSeconds(43200);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(ClaimTypes.Role, rol.Id),
                    //new Claim("permissions", JsonConvert.SerializeObject(rol.RolWebViews))
                }),
            IssuedAt = issuedAt,
            Expires = expirationTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return (ProccessCreateToken(tokenDescriptor), expirationTime);
    }

    private string ProccessCreateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.ReadToken(token);
        return tokenHandler.WriteToken(securityToken);
    }

    private string ProccessCreateToken(SecurityTokenDescriptor tokenDescriptor)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}