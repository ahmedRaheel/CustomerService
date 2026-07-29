namespace CustomerService.Domain.Entities;

public sealed class RegistrationStepHistory : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public RegistrationStep Step { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OccurredUtc { get; set; }
}
