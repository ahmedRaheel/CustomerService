using CustomerService.Api.Extensions;
using CustomerService.Application.Otp;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/registrations/{registrationId:guid}/otp")]
[Produces("application/json")]
public sealed class OtpController(ISender sender) : ControllerBase
{
    [HttpPost("email")]
    public async Task<ActionResult> SendEmail(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SendOtpCommand(registrationId, OtpChannel.Email),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("sms")]
    public async Task<ActionResult> SendSms(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SendOtpCommand(registrationId, OtpChannel.Sms),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("email/verify")]
    public async Task<ActionResult> VerifyEmail(
        Guid registrationId,
        VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new VerifyOtpCommand(
                registrationId,
                OtpChannel.Email,
                request.Otp,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("sms/verify")]
    public async Task<ActionResult> VerifySms(
        Guid registrationId,
        VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new VerifyOtpCommand(
                registrationId,
                OtpChannel.Sms,
                request.Otp,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        return this.ToActionResult(result);
    }
}
