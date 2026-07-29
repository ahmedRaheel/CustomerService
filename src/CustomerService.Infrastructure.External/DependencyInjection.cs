using CustomerService.Application.Abstractions.Notifications;using CustomerService.Infrastructure.External.Notifications;using CustomerService.Infrastructure.External.Options;using Microsoft.Extensions.Configuration;using Microsoft.Extensions.DependencyInjection;
namespace CustomerService.Infrastructure.External;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureExternal(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SmsOptions>().Bind(configuration.GetSection(SmsOptions.SectionName)).ValidateOnStart();
        services.AddOptions<TwilioOptions>().Bind(configuration.GetSection(TwilioOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IOtpService,OtpService>();
        services.AddSingleton<IPinService,PinService>();
        services.AddTransient<IEmailSender,SmtpEmailSender>();
        services.AddHttpClient<ISmsSender,TwilioSmsSender>();
        return services;
    }
}
