using CustomerService.Api.Extensions;
using CustomerService.Application.Terms;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/v1/terms")]
[Produces("application/json")]
public sealed class TermsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<TermDocumentDto>>>> GetAll(
        CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveTermsQuery(), ct);

        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<TermDocumentDto>>> Get(
        Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetTermQuery(id), ct);

        return this.ToActionResult(result);
    }
}
