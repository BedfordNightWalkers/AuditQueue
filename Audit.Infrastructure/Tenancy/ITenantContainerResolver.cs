namespace Audit.Infrastructure.Tenancy;

public interface ITenantContainerResolver
{
    string ResolveContainerName(string trustedClientId);

}