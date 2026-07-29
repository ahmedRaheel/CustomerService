using CustomerService.Domain.Entities;

namespace CustomerService.Domain.Dtos;

public sealed record StartRegistrationRequest(string Email, string MobileNumber, RegistrationType Type, string? NationalId);
public sealed record StartRegistrationResponse(Guid RegistrationId, RegistrationStatus Status);
public sealed record UpdateProfileRequest(string FullName, string? NationalId);
public sealed record VerifyOtpsRequest(string EmailOtp, string SmsOtp);
public sealed record SetPinRequest(string Pin, string ConfirmPin);
public sealed record SetPinResponse(Guid RegistrationId, bool PinConfigured, DateTime PinSetUtc);
public sealed record NotificationDeliveryDto(
    Guid Id,
    NotificationChannel Channel,
    string Destination,
    string TemplateCode,
    NotificationDeliveryStatus Status,
    int AttemptCount,
    string? ProviderMessageId,
    string? FailureReason,
    DateTime CreatedUtc,
    DateTime? SentUtc);
public sealed record RegistrationDto(
    Guid Id,
    RegistrationType Type,
    RegistrationStatus Status,
    string Email,
    string MobileNumber,
    string? NationalId,
    string? FullName,
    bool EmailVerified,
    bool SmsVerified,
    bool PinConfigured,
    DateTime? PinSetUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
