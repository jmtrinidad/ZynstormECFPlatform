using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Jobs;

public class ReceivedB2BMessagesCleanupJob
{
    private readonly IRepository<ReceivedB2BMessage> _receivedMessageRepository;
    private readonly ILogger<ReceivedB2BMessagesCleanupJob> _logger;

    public ReceivedB2BMessagesCleanupJob(
        IRepository<ReceivedB2BMessage> receivedMessageRepository,
        ILogger<ReceivedB2BMessagesCleanupJob> logger)
    {
        _receivedMessageRepository = receivedMessageRepository;
        _logger = logger;
    }

    public async Task CleanupOldMessagesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Received B2B Messages Cleanup Job...");

        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        // Find all records where ReceivedAtUtc is older than 30 days
        var oldMessages = await _receivedMessageRepository.Table
            .Where(m => m.ReceivedAtUtc < cutoffDate)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} messages older than 30 days to clean up.", oldMessages.Count);

        int dbDeleted = 0;

        foreach (var message in oldMessages)
        {
            try
            {
                // Delete from DB
                await _receivedMessageRepository.HardDeleteAsync(message);
                dbDeleted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up B2B message ID {MessageId} for ClientId {ClientId}", message.ReceivedB2BMessageId, message.ClientId);
            }
        }

        _logger.LogInformation("Finished Received B2B Messages Cleanup Job. Deleted {DbDeleted} database records.", dbDeleted);
    }
}
