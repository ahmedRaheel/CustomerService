CREATE TABLE Invoices (
    Id uniqueidentifier NOT NULL,
    InvoiceNumber nvarchar(50) NOT NULL,
    TotalAmount decimal(18,2) NOT NULL,
    CreatedAt datetime2 NOT NULL
);
