namespace Opplat.MultiTenant.Abstractions;

/// <summary>
/// Provides access to the current tenant context within a request scope.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public interface ITenantContext<TTenant> where TTenant : class
{
    /// <summary>
    /// Gets the current tenant for the request, or null if no tenant is resolved.
    /// </summary>
    TTenant? CurrentTenant { get; }

    /// <summary>
    /// Gets a value indicating whether a tenant has been resolved for the current request.
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// Sets the current tenant for the request scope.
    /// </summary>
    /// <param name="tenant">The tenant to set as current.</param>
    /// <exception cref="ArgumentNullException">Thrown when tenant is null.</exception>
    void SetTenant(TTenant tenant);

    /// <summary>
    /// Clears the current tenant from the request scope.
    /// </summary>
    void ClearTenant();
}