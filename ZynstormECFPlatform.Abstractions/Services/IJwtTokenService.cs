using System.Security.Claims;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IJwtTokenService
{
    ClaimsPrincipal GetPrincipalClaim(string token, string secret);

    int GetUserIdFromToken(string token, string secret);

    (string token, DateTime exp) CreateToken(User user, string secret, Role rol);
}