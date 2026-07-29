using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Shared;
using FluentValidation;
using MediatR;

namespace CustomerService.Application.Notifications.Queries;

public sealed record GetNotificationDeliveriesQuery(Guid RegistrationId)
    : IRequest<Result<IReadOnlyList<NotificationDeliveryDto>>>;

public sealed class GetNotificationDeliveriesValidator
    : AbstractValidator<GetNotificationDeliveriesQuery>
{
    public GetNotificationDeliveriesValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
    }
}

public sealed class GetNotificationDeliveriesHandler(
    IRegistrationQueryRepository queryRepository)
    : IRequestHandler<GetNotificationDeliveriesQuery, Result<IReadOnlyList<NotificationDeliveryDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationDeliveryDto>>> Handle(
        GetNotificationDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        var registration = await queryRepository.GetAsync(
            request.RegistrationId,
            cancellationToken);

        if (registration is null)
        {
            return Result.NotFound<IReadOnlyList<NotificationDeliveryDto>>(
                ResultMessages.RegistrationNotFound);
        }

        var deliveries = await queryRepository.GetDeliveriesAsync(
            request.RegistrationId,
            cancellationToken);
        var response = deliveries
            .Select(delivery => new NotificationDeliveryDto(
                delivery.Id,
                delivery.Channel,
                delivery.Destination,
                delivery.TemplateCode,
                delivery.Status,
                delivery.AttemptCount,
                delivery.ProviderMessageId,
                delivery.FailureReason,
                delivery.CreatedUtc,
                delivery.SentUtc))
            .ToList();

        return Result.Success<IReadOnlyList<NotificationDeliveryDto>>(
            response,
            ResultMessages.DeliveriesRetrieved);
    }
}
