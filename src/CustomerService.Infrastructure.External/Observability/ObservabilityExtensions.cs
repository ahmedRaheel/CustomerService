using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CustomerService.Infrastructure.External.Observability;
public static class ObservabilityExtensions
{
    public static IServiceCollection AddApplicationObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService("CustomerService")).WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddOtlpExporter()).WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddOtlpExporter());
        return services;
    }
}