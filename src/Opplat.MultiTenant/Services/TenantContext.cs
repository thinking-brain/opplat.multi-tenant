using Opplat.MultiTenant.Abstractions;

namespace Opplat.MultiTenant.Services;

/// <summary>
/// Provides scoped access to the current tenant within a request context.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class TenantContext<TTenant> : ITenantContext<TTenant> where TTenant : class
{
    private TTenant? _currentTenant;

    /// <inheritdoc />
    public TTenant? CurrentTenant => _currentTenant;

    /// <inheritdoc />
    public bool HasTenant => _currentTenant is not null;

    /// <inheritdoc />
    public void SetTenant(TTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        _currentTenant = tenant;
    }

    /// <inheritdoc />
    public void ClearTenant()
    {
        _currentTenant = null;
    }
}