namespace CustomerService.Domain.Dtos;
/// <summary>
/// Represents the detailed Invoice DTO used only when children/parents are explicitly requested.
/// </summary>
public sealed record InvoiceDetailDto(Guid Id, string InvoiceNumber, decimal TotalAmount, DateTime CreatedAt);