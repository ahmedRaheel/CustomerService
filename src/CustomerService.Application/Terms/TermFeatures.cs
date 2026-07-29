using MediatR;using CustomerService.Application.Abstractions.Persistence;using CustomerService.Domain.Dtos;using CustomerService.Domain.Exceptions;
namespace CustomerService.Application.Terms;
public sealed record GetActiveTermsQuery():IRequest<IReadOnlyList<TermDocumentDto>>;
public sealed class GetActiveTermsHandler(IRegistrationRepository repo):IRequestHandler<GetActiveTermsQuery,IReadOnlyList<TermDocumentDto>>{public async Task<IReadOnlyList<TermDocumentDto>> Handle(GetActiveTermsQuery r,CancellationToken ct)=>(await repo.GetActiveTermsAsync(ct)).Select(x=>new TermDocumentDto(x.Id,x.Code,x.Title,x.Content,x.Version,x.IsRequired,x.EffectiveFromUtc)).ToList();}
public sealed record GetTermQuery(Guid Id):IRequest<TermDocumentDto>;
public sealed class GetTermHandler(IRegistrationRepository repo):IRequestHandler<GetTermQuery,TermDocumentDto>{public async Task<TermDocumentDto> Handle(GetTermQuery r,CancellationToken ct){var x=await repo.GetTermAsync(r.Id,ct)??throw new NotFoundException("Term document not found.");return new(x.Id,x.Code,x.Title,x.Content,x.Version,x.IsRequired,x.EffectiveFromUtc);}}
