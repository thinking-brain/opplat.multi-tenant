using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Opplat.MultiTenant.Abstractions;

namespace Opplat.MultiTenant.Resolvers;

/// <summary>
/// Resolves tenants using multiple tenant resolution strategies in priority order.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class CompositeTenantResolver<TTenant> : ITenantResolver<TTenant> where TTenant : class
{
    private readonly IEnumerable<ITenantResolver<TTenant>> _resolvers;
    private readonly ILogger<CompositeTenantResolver<TTenant>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTenantResolver{TTenant}"/> class.
    /// </summary>
    /// <param name="resolvers">The collection of tenant resolvers to use in priority order.</param>
    /// <param name="logger">The logger.</param>
    public CompositeTenantResolver(
        IEnumerable<ITenantResolver<TTenant>> resolvers,
        ILogger<CompositeTenantResolver<TTenant>> logger)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TTenant?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("Starting tenant resolution using composite resolver with {ResolverCount} resolvers", 
            _resolvers.Count());

        foreach (var resolver in _resolvers)
        {
            try
            {
                if (await resolver.CanResolveAsync(context, cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Attempting tenant resolution using {ResolverType}", 
                        resolver.GetType().Name);

                    var tenant = await resolver.ResolveTenantAsync(context, cancellationToken).ConfigureAwait(false);
                    
                    if (tenant is not null)
                    {
                        _logger.LogDebug("Successfully resolved tenant using {ResolverType}", 
                            resolver.GetType().Name);
                        return tenant;
                    }

                    _logger.LogDebug("Resolver {ResolverType} could not resolve tenant", 
                        resolver.GetType().Name);
                }
                else
                {
                    _logger.LogDebug("Resolver {ResolverType} cannot handle this request", 
                        resolver.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resolving tenant using {ResolverType}", 
                    resolver.GetType().Name);
                // Continue to next resolver instead of failing the entire resolution
            }
        }

        _logger.LogWarning("No resolver could resolve a tenant for the current request");
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> CanResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var resolver in _resolvers)
        {
            try
            {
                if (await resolver.CanResolveAsync(context, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if {ResolverType} can resolve tenant", 
                    resolver.GetType().Name);
                // Continue checking other resolvers
            }
        }

        return false;
    }
}