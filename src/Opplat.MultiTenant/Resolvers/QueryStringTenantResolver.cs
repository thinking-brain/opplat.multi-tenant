using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opplat.MultiTenant.Abstractions;
using Opplat.MultiTenant.Configuration;

namespace Opplat.MultiTenant.Resolvers;

/// <summary>
/// Resolves tenants from query string parameters.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class QueryStringTenantResolver<TTenant> : ITenantResolver<TTenant> where TTenant : class
{
    private readonly ITenantProvider<TTenant> _tenantProvider;
    private readonly ILogger<QueryStringTenantResolver<TTenant>> _logger;
    private readonly MultiTenantOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryStringTenantResolver{TTenant}"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The multi-tenant options.</param>
    public QueryStringTenantResolver(
        ITenantProvider<TTenant> tenantProvider,
        ILogger<QueryStringTenantResolver<TTenant>> logger,
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

        if (!context.Request.Query.TryGetValue(_options.TenantQueryParameterName, out var tenantIdValues))
        {
            _logger.LogDebug("Tenant query parameter '{ParameterName}' not found in request", 
                _options.TenantQueryParameterName);
            return null;
        }

        var tenantId = tenantIdValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogDebug("Tenant query parameter '{ParameterName}' is empty", 
                _options.TenantQueryParameterName);
            return null;
        }

        _logger.LogDebug("Resolving tenant with ID '{TenantId}' from query parameter '{ParameterName}'", 
            tenantId, _options.TenantQueryParameterName);

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
        
        var canResolve = context.Request.Query.ContainsKey(_options.TenantQueryParameterName);
        return Task.FromResult(canResolve);
    }
}