# Customer Registration API

Clean Architecture and CQRS registration API with separate email and SMS OTP delivery, one combined OTP verification endpoint, six-digit PIN setup, notification templates, and delivery tracking.

## Setup

1. Run `database/create-database.sql` against SQL Server.
2. Configure `ConnectionStrings:DefaultConnection`.
3. Store the Outlook SMTP password outside source control:
   ```bash
   dotnet user-secrets set "Email:Password" "your-password" --project src/CustomerService.Api
   ```
4. Configure Twilio `AccountSid`, `AuthToken`, and either `FromNumber` or `MessagingServiceSid`.
5. Run the API.

SMTP and Twilio settings are consumed through `IOptionsMonitor<T>` so refreshed configuration is read without recreating the sender services.

## Registration flow

```text
POST /api/v1/registrations
POST /api/v1/registrations/{id}/otp/email
POST /api/v1/registrations/{id}/otp/sms
POST /api/v1/registrations/{id}/otp/verify
PUT  /api/v1/registrations/{id}/profile
PUT  /api/v1/registrations/{id}/pin
POST /api/v1/registrations/{id}/complete
GET  /api/v1/registrations/{id}
GET  /api/v1/registrations/{id}/notification-deliveries
```

The PIN endpoint requires `Pin` and `ConfirmPin`, both containing the same six digits. The PIN is never stored as plain text. The database stores a PBKDF2 hash and a unique salt.

## Notification tracking

The outbox implementation has been removed. OTP notifications are sent synchronously and every attempt is recorded in `notify.NotificationDeliveries` with:

- channel and destination
- template code
- attempt count
- pending, sending, sent, or failed status
- provider message ID
- failure reason
- created, updated, and sent timestamps

Email and SMS content is loaded from active rows in `notify.NotificationTemplates`. Template variables use `{{VariableName}}`.

## API responses and logging

The APIs return a consistent response envelope containing `success`, `message`, `data`, `traceId`, and `timestampUtc`. Errors contain an error code, message, trace ID, and optional validation errors.

Serilog is configured in `CustomerService.Infrastructure.External/Logging/Serilog`. Logs are written to console and daily files under `logs/customer-service-YYYYMMDD.log`, with rolling by date and size and enriched request, trace, process, machine, environment, and correlation properties.
