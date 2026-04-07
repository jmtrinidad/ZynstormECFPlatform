using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IJwtTokenService
{
    ClaimsPrincipal GetPrincipalClaim(string token, string secret);

    int GetUserIdFromToken(string token, string secret);

    TokenDto CreateToken(User user, IdentityRole rol);
}