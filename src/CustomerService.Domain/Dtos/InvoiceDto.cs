namespace CustomerService.Domain.Dtos;
/// <summary>
/// Represents the flat Invoice data transfer object. It intentionally excludes parent/child navigation data.
/// </summary>
public sealed record InvoiceDto(Guid Id, string InvoiceNumber, decimal TotalAmount, DateTime CreatedAt);