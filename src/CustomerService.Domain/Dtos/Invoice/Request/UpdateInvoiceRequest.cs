namespace CustomerService.Domain.Dtos.Invoice.Request;
public sealed record UpdateInvoiceRequest(string InvoiceNumber, decimal TotalAmount);