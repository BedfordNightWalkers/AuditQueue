using Audit.Application.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPolicyService, PolicyService>();
        return services;
    }
}