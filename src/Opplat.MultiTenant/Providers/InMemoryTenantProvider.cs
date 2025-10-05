using Microsoft.Extensions.Options;
using Opplat.MultiTenant.Abstractions;
using Opplat.MultiTenant.Configuration;
using System.Collections.Concurrent;

namespace Opplat.MultiTenant.Providers;

/// <summary>
/// An in-memory implementation of <see cref="ITenantProvider{TTenant}"/> for testing and simple scenarios.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class InMemoryTenantProvider<TTenant> : ITenantProvider<TTenant> where TTenant : class
{
    private readonly ConcurrentDictionary<string, TTenant> _tenants;
    private readonly MultiTenantOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTenantProvider{TTenant}"/> class.
    /// </summary>
    /// <param name="options">The multi-tenant options.</param>
    public InMemoryTenantProvider(IOptions<MultiTenantOptions> options)
    {
        _options = options.Value;
        _tenants = new ConcurrentDictionary<string, TTenant>(
            _options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTenantProvider{TTenant}"/> class with initial tenants.
    /// </summary>
    /// <param name="options">The multi-tenant options.</param>
    /// <param name="tenants">The initial collection of tenants.</param>
    public InMemoryTenantProvider(IOptions<MultiTenantOptions> options, IEnumerable<TTenant> tenants)
        : this(options)
    {
        ArgumentNullException.ThrowIfNull(tenants);

        foreach (var tenant in tenants)
        {
            var tenantId = GetTenantId(tenant);
            if (!string.IsNullOrEmpty(tenantId))
            {
                _tenants.TryAdd(tenantId, tenant);
            }
        }
    }

    /// <inheritdoc />
    public Task<TTenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    /// <inheritdoc />
    public Task<IEnumerable<TTenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<TTenant>>(_tenants.Values.ToList());
    }

    /// <inheritdoc />
    public Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var exists = _tenants.ContainsKey(tenantId);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Adds a tenant to the provider.
    /// </summary>
    /// <param name="tenant">The tenant to add.</param>
    /// <returns><see langword="true"/> if the tenant was added; <see langword="false"/> if a tenant with the same ID already exists.</returns>
    public bool AddTenant(TTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var tenantId = GetTenantId(tenant);
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new ArgumentException("Tenant must have a valid ID", nameof(tenant));
        }

        return _tenants.TryAdd(tenantId, tenant);
    }

    /// <summary>
    /// Updates a tenant in the provider.
    /// </summary>
    /// <param name="tenant">The tenant to update.</param>
    /// <returns><see langword="true"/> if the tenant was updated; <see langword="false"/> if the tenant was not found.</returns>
    public bool UpdateTenant(TTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var tenantId = GetTenantId(tenant);
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new ArgumentException("Tenant must have a valid ID", nameof(tenant));
        }

        if (_tenants.ContainsKey(tenantId))
        {
            _tenants[tenantId] = tenant;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a tenant from the provider.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant to remove.</param>
    /// <returns><see langword="true"/> if the tenant was removed; <see langword="false"/> if the tenant was not found.</returns>
    public bool RemoveTenant(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return _tenants.TryRemove(tenantId, out _);
    }

    /// <summary>
    /// Gets the tenant count.
    /// </summary>
    public int Count => _tenants.Count;

    /// <summary>
    /// Clears all tenants.
    /// </summary>
    public void Clear()
    {
        _tenants.Clear();
    }

    private static string GetTenantId(TTenant tenant) =>
        tenant switch
        {
            ITenant t => t.Id,
            _ => tenant.ToString() ?? string.Empty
        };
}