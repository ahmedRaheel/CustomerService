using CustomerService.Domain.Dtos;
using CustomerService.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CustomerService.Api.Middlewares;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, message, validationErrors) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}, Path: {RequestPath}",
                httpContext.TraceIdentifier,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed with status {StatusCode}. TraceId: {TraceId}, Path: {RequestPath}",
                statusCode,
                httpContext.TraceIdentifier,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiErrorResponse.Create(
            code,
            message,
            httpContext.TraceIdentifier,
            validationErrors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Code, string Message, IReadOnlyDictionary<string, string[]>? Errors)
        MapException(Exception exception) => exception switch
        {
            ValidationException validationException =>
                (StatusCodes.Status400BadRequest,
                 "validation_error",
                 "One or more validation errors occurred.",
                 validationException.Errors
                     .GroupBy(error => error.PropertyName)
                     .ToDictionary(
                         group => group.Key,
                         group => group.Select(error => error.ErrorMessage).Distinct().ToArray())),

            NotFoundException =>
                (StatusCodes.Status404NotFound, "not_found", exception.Message, null),

            InvalidOperationException =>
                (StatusCodes.Status400BadRequest, "business_rule_violation", exception.Message, null),

            OperationCanceledException =>
                (StatusCodes.Status499ClientClosedRequest, "request_cancelled", "The request was cancelled.", null),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "internal_server_error",
                 "An unexpected error occurred. Use the trace ID when contacting support.",
                 null)
        };
}
