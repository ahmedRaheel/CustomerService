using CustomerService.Api.Health;
using CustomerService.Api.Middlewares;
using CustomerService.Application;
using CustomerService.Infrastructure.Data;
using CustomerService.Infrastructure.External;
using CustomerService.Infrastructure.External.Logging.Serilog;
using CustomerService.Infrastructure.External.Observability;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ConfigureCustomerServiceSerilog(builder.Configuration, builder.Environment)
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger, dispose: true);

try
{
    Log.Information("Starting {ApplicationName}", builder.Environment.ApplicationName);

    builder.Services.AddOpenApi();
    builder.Services.AddControllers();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructureData(builder.Configuration);
    builder.Services.AddInfrastructureExternal(builder.Configuration);
    builder.Services.AddApplicationObservability(builder.Configuration);
    builder.Services.AddApiHealthChecks();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseMiddleware<RequestLogContextMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("Host", httpContext.Request.Host.Value);
            diagnosticContext.Set("Scheme", httpContext.Request.Scheme);
            diagnosticContext.Set("Protocol", httpContext.Request.Protocol);
            diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value ?? string.Empty);
            diagnosticContext.Set("Endpoint", httpContext.GetEndpoint()?.DisplayName ?? "unknown");
            diagnosticContext.Set("UserName", httpContext.User.Identity?.Name ?? "anonymous");
        };
    });

    app.UseExceptionHandler();
    app.MapApiHealthChecks();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
