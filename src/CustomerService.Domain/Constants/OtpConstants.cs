namespace CustomerService.Domain.Constants;

public static class OtpConstants
{
    public const int CodeLength = 6;
    public const int ExpiryMinutes = 10;
    public const int MaxAttempts = 5;
    public const int ResendCooldownSeconds = 60;
    public const int HourlySendLimit = 5;
    public const int HourWindow = -1;
    public const string SixDigitPattern = "^[0-9]{6}$";
}
