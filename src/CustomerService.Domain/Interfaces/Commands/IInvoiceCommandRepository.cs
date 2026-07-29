using CustomerService.Domain.Entities;

namespace CustomerService.Domain.Interfaces.Commands;

/// <summary>
/// Provides write operations for Invoice.
/// </summary>
public interface IInvoiceCommandRepository
{
    /// <summary>
    /// Inserts a Invoice entity asynchronously.
    /// </summary>
    Task InsertAsync(InvoiceEntity entity, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates a Invoice entity asynchronously.
    /// </summary>
    Task UpdateAsync(Guid id, InvoiceEntity entity, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes a Invoice entity asynchronously.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
