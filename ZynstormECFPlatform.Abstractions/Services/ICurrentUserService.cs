namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
