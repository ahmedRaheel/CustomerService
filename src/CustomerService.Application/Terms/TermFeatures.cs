using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Shared;
using FluentValidation;
using MediatR;

namespace CustomerService.Application.Terms;

public sealed record GetActiveTermsQuery
    : IRequest<Result<IReadOnlyList<TermDocumentDto>>>;

public sealed class GetActiveTermsHandler(IRegistrationQueryRepository queryRepository)
    : IRequestHandler<GetActiveTermsQuery, Result<IReadOnlyList<TermDocumentDto>>>
{
    public async Task<Result<IReadOnlyList<TermDocumentDto>>> Handle(
        GetActiveTermsQuery r,
        CancellationToken ct)
    {
        var terms = await queryRepository.GetActiveTermsAsync(ct);
        var response = terms
            .Select(x => new TermDocumentDto(
                x.Id,
                x.Code,
                x.Title,
                x.Content,
                x.Version,
                x.IsRequired,
                x.EffectiveFromUtc))
            .ToList();

        return Result.Success<IReadOnlyList<TermDocumentDto>>(
            response,
            ResultMessages.TermsRetrieved);
    }
}

public sealed record GetTermQuery(Guid Id) : IRequest<Result<TermDocumentDto>>;

public sealed class GetTermValidator : AbstractValidator<GetTermQuery>
{
    public GetTermValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetTermHandler(IRegistrationQueryRepository queryRepository)
    : IRequestHandler<GetTermQuery, Result<TermDocumentDto>>
{
    public async Task<Result<TermDocumentDto>> Handle(
        GetTermQuery r,
        CancellationToken ct)
    {
        var term = await queryRepository.GetTermAsync(r.Id, ct);

        if (term is null)
            return Result.NotFound<TermDocumentDto>(ResultMessages.TermDocumentNotFound);

        var response = new TermDocumentDto(
            term.Id,
            term.Code,
            term.Title,
            term.Content,
            term.Version,
            term.IsRequired,
            term.EffectiveFromUtc);

        return Result.Success(response, ResultMessages.TermRetrieved);
    }
}
