using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services;

public sealed class SerialGeneratorService(IGeneratedSerialService serialRepository) : ISerialGeneratorService
{
    public async Task<GeneratedSerialResult> GenerateAsync(
        string? description,
        CancellationToken cancellationToken = default)
    {
        string serial;
        string hash;

        do
        {
            serial = SerialCodeGenerator.Generate();
            hash = SerialCodeGenerator.ComputeHash(serial);
        }
        while (await serialRepository.Table.AnyAsync(item => item.SerialHash == hash, cancellationToken));

        var entity = new GeneratedSerial
        {
            SerialHash = hash,
            SerialSuffix = serial[^5..],
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true
        };

        await serialRepository.InsertAsync(entity);
        return new GeneratedSerialResult(serial, entity);
    }

    public async Task<bool> ValidateAndConsumeAsync(string serial, CancellationToken cancellationToken = default)
    {
        var normalized = SerialCodeGenerator.Normalize(serial);
        if (normalized.Length != 20 || normalized.Any(character => !"ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(character)))
            return false;

        var hash = SerialCodeGenerator.ComputeHash(normalized);
        var usedAtUtc = DateTime.UtcNow;

        var affectedRows = await serialRepository.Table
            .Where(item => item.SerialHash == hash && item.IsActive && !item.IsUsed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.IsUsed, true)
                    .SetProperty(item => item.UsedAtUtc, usedAtUtc)
                    .SetProperty(item => item.LastUpdateUtc, usedAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }
}
