using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ISerialGeneratorService
{
    Task<GeneratedSerialResult> GenerateAsync(string? description, CancellationToken cancellationToken = default);

    Task<bool> ValidateAndConsumeAsync(string serial, CancellationToken cancellationToken = default);
}

public sealed record GeneratedSerialResult(string Serial, GeneratedSerial Entity);
