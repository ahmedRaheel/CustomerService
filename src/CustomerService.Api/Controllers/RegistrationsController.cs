using CustomerService.Api.Extensions;
using CustomerService.Application.Registrations;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/registrations")]
[Produces("application/json")]
public sealed class RegistrationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<StartRegistrationResponse>>> Start(
        StartRegistrationRequest r,
        CancellationToken ct)
    {
        var result = await sender.Send(new StartRegistrationCommand(
            r.Email,
            r.MobileNumber,
            r.Type,
            r.NationalId,
            r.LegacyCustomerId), ct);

        return this.ToActionResult(result);
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<ActionResult> Profile(
        Guid id,
        UpdateProfileRequest r,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRegistrationProfileCommand(
            id,
            r.FullName,
            r.NationalId), ct);

        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/terms/accept")]
    public async Task<ActionResult> Accept(
        Guid id,
        AcceptTermsRequest r,
        CancellationToken ct)
    {
        var result = await sender.Send(new AcceptTermsCommand(
            id,
            r.TermIds,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        return this.ToActionResult(result);
    }

    [HttpPut("{id:guid}/pin")]
    public async Task<ActionResult> Pin(
        Guid id,
        SetPinRequest r,
        CancellationToken ct)
    {
        var result = await sender.Send(new SetPinCommand(
            id,
            r.Pin,
            r.ConfirmPin), ct);

        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteRegistrationCommand(id), ct);

        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> Cancel(
        Guid id,
        CancelRegistrationRequest r,
        CancellationToken ct)
    {
        var result = await sender.Send(new CancelRegistrationCommand(
            id,
            r.Reason), ct);

        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<RegistrationDto>>> Get(
        Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetRegistrationQuery(id), ct);

        return this.ToActionResult(result);
    }
}
