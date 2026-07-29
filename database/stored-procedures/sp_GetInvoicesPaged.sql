CREATE PROCEDURE sp_GetInvoicesPaged AS BEGIN SELECT Id, InvoiceNumber, TotalAmount, CreatedAt FROM Invoices; END
