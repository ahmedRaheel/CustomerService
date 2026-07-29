using CustomerService.Domain.Entities;
namespace CustomerService.Application.Abstractions.Notifications;
public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, CancellationToken ct);
    Task<NotificationTemplate?> GetTemplateAsync(string code, NotificationChannel channel, CancellationToken ct);
    Task MarkSentAsync(Guid id, string? providerMessageId, CancellationToken ct);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct);
}
