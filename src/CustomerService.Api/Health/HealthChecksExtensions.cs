namespace CustomerService.Api.Health;
public static class HealthChecksExtensions
{
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IEndpointRouteBuilder MapApiHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready");
        return app;
    }
}