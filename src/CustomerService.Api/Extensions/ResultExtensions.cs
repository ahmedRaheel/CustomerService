using CustomerService.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult ToActionResult(
        this ControllerBase controller,
        Result result)
    {
        return controller.StatusCode(result.StatusCode, result);
    }

    public static ActionResult<Result<T>> ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result)
    {
        return controller.StatusCode(result.StatusCode, result);
    }
}
