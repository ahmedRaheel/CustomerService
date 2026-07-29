namespace CustomerService.Domain.Entities;

public sealed class CustomerAccount : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? FullName { get; set; }
    public string? LegacyCustomerId { get; set; }
    public bool IsMigrated { get; set; }
    public DateTime CreatedUtc { get; set; }
}
