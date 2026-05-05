using Microsoft.AspNetCore.SignalR;

namespace ZynstormECFPlatform.Common.Hubs;

public class CertificationHub : Hub
{
    public async Task SubscribeJob(string jobId)
    {
        if (!string.IsNullOrWhiteSpace(jobId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"cert-job:{jobId}");
    }

    public async Task UnsubscribeJob(string jobId)
    {
        if (!string.IsNullOrWhiteSpace(jobId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cert-job:{jobId}");
    }

    public async Task SubscribeClient(string clientGuidId)
    {
        if (!string.IsNullOrWhiteSpace(clientGuidId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"cert-client:{clientGuidId}");
    }

    public async Task UnsubscribeClient(string clientGuidId)
    {
        if (!string.IsNullOrWhiteSpace(clientGuidId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cert-client:{clientGuidId}");
    }
}
