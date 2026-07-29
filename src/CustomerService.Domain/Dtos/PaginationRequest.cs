namespace CustomerService.Domain.Dtos;
public sealed record PaginationRequest(int PageNumber = 1, int PageSize = 20, string? Search = null);