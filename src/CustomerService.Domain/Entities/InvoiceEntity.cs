namespace CustomerService.Domain.Entities;
/// <summary>
/// Represents the Invoice domain entity.
/// </summary>
public sealed class InvoiceEntity : BaseEntity
{
    /// <summary>
    /// Gets the Id value.
    /// </summary>
    /// <summary>
    /// Gets the InvoiceNumber value.
    /// </summary>
    public string InvoiceNumber { get; private set; } = string.Empty;
    /// <summary>
    /// Gets the TotalAmount value.
    /// </summary>
    public decimal TotalAmount { get; private set; }
    /// <summary>
    /// Gets the CreatedAt value.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the InvoiceEntity class for EF Core.
    /// </summary>
    private InvoiceEntity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvoiceEntity class.
    /// </summary>
    private InvoiceEntity(string invoiceNumber, decimal totalAmount)
    {
        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        TotalAmount = totalAmount;
    }

    /// <summary>
    /// Creates a new InvoiceEntity.
    /// </summary>
    public static InvoiceEntity Create(string invoiceNumber, decimal totalAmount)
    {
        return new InvoiceEntity(invoiceNumber, totalAmount);
    }

    /// <summary>
    /// Updates the InvoiceEntity state.
    /// </summary>
    public void Update(string invoiceNumber, decimal totalAmount)
    {
        Id = Guid.NewGuid();
        InvoiceNumber = invoiceNumber;
        TotalAmount = totalAmount;
    }
}