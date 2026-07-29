using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CustomerService.Infrastructure.External.Logging.Serilog;

public static class SerilogConfiguration
{
    public static LoggerConfiguration ConfigureCustomerServiceSerilog(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment) =>
        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", environment.ApplicationName)
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.With(new ProcessEnricher())
            .Enrich.With(new ThreadEnricher());

    private sealed class ProcessEnricher : ILogEventEnricher
    {
        private static readonly LogEventProperty ProcessId =
            new("ProcessId", new ScalarValue(Environment.ProcessId));

        private static readonly LogEventProperty ProcessName =
            new("ProcessName", new ScalarValue(Process.GetCurrentProcess().ProcessName));

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(ProcessId);
            logEvent.AddPropertyIfAbsent(ProcessName);
        }
    }

    private sealed class ThreadEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("ThreadId", Environment.CurrentManagedThreadId));
        }
    }
}
