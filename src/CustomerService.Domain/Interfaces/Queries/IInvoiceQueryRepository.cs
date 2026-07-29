using CustomerService.Domain.Dtos;
using CustomerService.Domain.Entities;

namespace CustomerService.Domain.Interfaces.Queries;

/// <summary>
/// Provides read operations for Invoice.
/// </summary>
public interface IInvoiceQueryRepository
{
    /// <summary>
    /// Gets a Invoice domain entity by id asynchronously.
    /// </summary>
    Task<InvoiceEntity?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets a flat Invoice by id asynchronously.
    /// </summary>
    Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets a detailed Invoice graph by id asynchronously when the caller explicitly needs children/parents.
    /// </summary>
    Task<InvoiceDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets paged Invoice records asynchronously.
    /// </summary>
    Task<PagedResult<InvoiceDto>> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default);
}
