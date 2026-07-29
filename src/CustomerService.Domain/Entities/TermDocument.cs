namespace CustomerService.Domain.Entities;

public sealed class TermDocument : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
