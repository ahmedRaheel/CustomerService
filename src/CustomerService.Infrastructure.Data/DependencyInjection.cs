using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Infrastructure.Data.Commands;
using CustomerService.Infrastructure.Data.Persistence.Context;
using CustomerService.Infrastructure.Data.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Infrastructure.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(DatabaseConstants.DefaultConnection)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationDbConnectionFactory, ApplicationDbConnectionFactory>();
        services.AddScoped<IRegistrationCommandRepository, RegistrationCommandRepository>();
        services.AddScoped<IRegistrationQueryRepository, RegistrationQueryRepository>();

        return services;
    }
}
