using CustomerService.Api.Extensions;
using CustomerService.Application.Notifications.Queries;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/registrations/{registrationId:guid}/notification-deliveries")]
[Produces("application/json")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<NotificationDeliveryDto>>>> GetDeliveries(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetNotificationDeliveriesQuery(registrationId),
            cancellationToken);

        return this.ToActionResult(result);
    }
}
