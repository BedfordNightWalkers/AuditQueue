using Microsoft.Extensions.Configuration;

namespace Audit.Infrastructure.Tenancy;

public sealed class ConfigurationTenantContainerResolver(
    IConfiguration configuration) : ITenantContainerResolver
{
    public string ResolveContainerName(string trustedClientId)
    {
        var mapping = configuration.GetSection(
                "TenantContainers")
            .Get<Dictionary<string, string>>() ?? new();

        return mapping.TryGetValue(trustedClientId, out var container)
            ? container
            : throw new InvalidOperationException(
                $"No audit container configured for client '{trustedClientId}'");
    }
}