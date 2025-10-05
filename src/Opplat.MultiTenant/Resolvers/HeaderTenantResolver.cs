using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opplat.MultiTenant.Abstractions;
using Opplat.MultiTenant.Configuration;

namespace Opplat.MultiTenant.Resolvers;

/// <summary>
/// Resolves tenants from HTTP request headers.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class HeaderTenantResolver<TTenant> : ITenantResolver<TTenant> where TTenant : class
{
    private readonly ITenantProvider<TTenant> _tenantProvider;
    private readonly ILogger<HeaderTenantResolver<TTenant>> _logger;
    private readonly MultiTenantOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderTenantResolver{TTenant}"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The multi-tenant options.</param>
    public HeaderTenantResolver(
        ITenantProvider<TTenant> tenantProvider,
        ILogger<HeaderTenantResolver<TTenant>> logger,
        IOptions<MultiTenantOptions> options)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<TTenant?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Headers.TryGetValue(_options.TenantHeaderName, out var tenantIdValues))
        {
            _logger.LogDebug("Tenant header '{HeaderName}' not found in request", _options.TenantHeaderName);
            return null;
        }

        var tenantId = tenantIdValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogDebug("Tenant header '{HeaderName}' is empty", _options.TenantHeaderName);
            return null;
        }

        _logger.LogDebug("Resolving tenant with ID '{TenantId}' from header '{HeaderName}'", 
            tenantId, _options.TenantHeaderName);

        var tenant = await _tenantProvider.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        
        if (tenant is null)
        {
            _logger.LogWarning("Tenant with ID '{TenantId}' not found", tenantId);
        }
        else
        {
            _logger.LogDebug("Successfully resolved tenant with ID '{TenantId}'", tenantId);
        }

        return tenant;
    }

    /// <inheritdoc />
    public Task<bool> CanResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var canResolve = context.Request.Headers.ContainsKey(_options.TenantHeaderName);
        return Task.FromResult(canResolve);
    }
}