namespace Opplat.MultiTenant.Abstractions;

/// <summary>
/// Provides access to tenant data from a data source.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public interface ITenantProvider<TTenant> where TTenant : class
{
    /// <summary>
    /// Gets a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the tenant with the specified identifier, or <see langword="null"/> if not found.
    /// </returns>
    Task<TTenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tenants.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of all available tenants.
    /// </returns>
    Task<IEnumerable<TTenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a tenant with the specified identifier exists.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// <see langword="true"/> if the tenant exists; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default);
}