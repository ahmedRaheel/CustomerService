namespace CustomerService.Domain.Dtos.Invoice.Request;
public sealed record CreateInvoiceRequest(string InvoiceNumber, decimal TotalAmount);