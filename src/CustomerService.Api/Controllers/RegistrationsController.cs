using CustomerService.Application.Registrations;
using CustomerService.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/registrations")]
[Produces("application/json")]
public sealed class RegistrationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StartRegistrationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StartRegistrationResponse>>> StartAsync(
        [FromBody] StartRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new StartRegistrationCommand(request.Email, request.MobileNumber, request.Type, request.NationalId),
            cancellationToken);      

        var response = Success(result, "Registration started successfully.");

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = result.RegistrationId },
            response);
    }

    [HttpPost("{id:guid}/otp/email")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> SendEmailOtpAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SendEmailOtpCommand(id), cancellationToken);
        
        return Ok(Success<object?>(null, "Email OTP sent successfully."));
    }

    [HttpPost("{id:guid}/otp/sms")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> SendSmsOtpAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SendSmsOtpCommand(id), cancellationToken);
        
        return Ok(Success<object?>(null, "SMS OTP sent successfully."));
    }

    [HttpPost("{id:guid}/otp/verify")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> VerifyOtpsAsync(
        [FromRoute] Guid id,
        [FromBody] VerifyOtpsRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new VerifyBothOtpsCommand(id, request.EmailOtp, request.SmsOtp),
            cancellationToken);
      
        return Ok(Success<object?>(null, "Email and SMS OTPs verified successfully."));
    }

    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateProfileAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateRegistrationProfileCommand(id, request.FullName, request.NationalId),
            cancellationToken);      
        return Ok(Success<object?>(null, "Registration profile updated successfully."));
    }


    [HttpPut("{id:guid}/pin")]
    [ProducesResponseType(typeof(ApiResponse<SetPinResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SetPinResponse>>> SetPinAsync(
        [FromRoute] Guid id,
        [FromBody] SetPinRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetRegistrationPinCommand(id, request.Pin, request.ConfirmPin),
            cancellationToken);

        return Ok(Success(result, "Six-digit PIN configured successfully."));
    }

    [HttpGet("{id:guid}/notification-deliveries")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDeliveryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDeliveryDto>>>> GetNotificationDeliveriesAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNotificationDeliveriesQuery(id), cancellationToken);
        return Ok(Success(result, "Notification delivery history retrieved successfully."));
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> CompleteAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CompleteRegistrationCommand(id), cancellationToken);       
        return Ok(Success<object?>(null, "Registration completed successfully."));
    }

    [HttpGet("{id:guid}", Name = nameof(GetByIdAsync))]
    [ProducesResponseType(typeof(ApiResponse<RegistrationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> GetByIdAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var registration = await sender.Send(new GetRegistrationQuery(id), cancellationToken);
        return Ok(Success(registration, "Registration retrieved successfully."));
    }

    private ApiResponse<T> Success<T>(T? data, string message) =>
        ApiResponse<T>.Ok(data, message, HttpContext.TraceIdentifier);
}
