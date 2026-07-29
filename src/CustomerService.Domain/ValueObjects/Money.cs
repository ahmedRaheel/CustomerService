namespace CustomerService.Domain.ValueObjects;

/// <summary>
/// Immutable Money value object. It protects invariants at the domain boundary.
/// </summary>
public sealed class Money : ValueObject
{
    private Money(decimal value) => Value = value;

    public decimal Value { get; }

    public static Money Create(decimal value)
    {
        // Add domain-specific invariant checks here when needed.
        return new Money(value);
    }

    public static bool TryCreate(decimal value, out Money? result)
    {
        try
        {
            result = Create(value);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public override string ToString() => Value.ToString();

    public static implicit operator decimal(Money value) => value.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}