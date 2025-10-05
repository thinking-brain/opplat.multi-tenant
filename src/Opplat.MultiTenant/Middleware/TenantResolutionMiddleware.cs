using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opplat.MultiTenant.Abstractions;
using Opplat.MultiTenant.Configuration;
using Opplat.MultiTenant.Exceptions;

namespace Opplat.MultiTenant.Middleware;

/// <summary>
/// Middleware for resolving tenants from HTTP requests and setting the tenant context.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class TenantResolutionMiddleware<TTenant> where TTenant : class
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware<TTenant>> _logger;
    private readonly MultiTenantOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionMiddleware{TTenant}"/> class.
    /// </summary>
    /// <param name="next">The next request delegate in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The multi-tenant options.</param>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware<TTenant>> logger,
        IOptions<MultiTenantOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Invokes the middleware to resolve the tenant for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tenantResolver = context.RequestServices.GetRequiredService<ITenantResolver<TTenant>>();
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext<TTenant>>();

        _logger.LogDebug("Starting tenant resolution for request: {Method} {Path}", 
            context.Request.Method, context.Request.Path);

        try
        {
            var tenant = await tenantResolver.ResolveTenantAsync(context).ConfigureAwait(false);

            if (tenant is not null)
            {
                tenantContext.SetTenant(tenant);
                
                var tenantId = GetTenantId(tenant);
                _logger.LogDebug("Successfully resolved and set tenant: {TenantId}", tenantId);
                
                // Add tenant ID to response headers for debugging
                context.Response.Headers.Append("X-Resolved-Tenant", tenantId);
            }
            else
            {
                _logger.LogDebug("No tenant could be resolved for the current request");
                await HandleTenantNotFound(context, tenantContext).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during tenant resolution");
            
            if (_options.RequireTenant)
            {
                throw new TenantResolutionException("Failed to resolve tenant for the current request", ex);
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    private async Task HandleTenantNotFound(HttpContext context, ITenantContext<TTenant> tenantContext)
    {
        switch (_options.NotFoundAction)
        {
            case TenantNotFoundAction.ThrowException:
                if (_options.RequireTenant)
                {
                    throw new TenantNotFoundException("No tenant could be resolved for the current request");
                }
                break;

            case TenantNotFoundAction.UseDefault:
                if (!string.IsNullOrEmpty(_options.DefaultTenantId))
                {
                    var tenantProvider = context.RequestServices.GetRequiredService<ITenantProvider<TTenant>>();
                    var defaultTenant = await tenantProvider.GetTenantAsync(_options.DefaultTenantId).ConfigureAwait(false);
                    
                    if (defaultTenant is not null)
                    {
                        tenantContext.SetTenant(defaultTenant);
                        _logger.LogDebug("Using default tenant: {TenantId}", _options.DefaultTenantId);
                    }
                    else
                    {
                        _logger.LogWarning("Default tenant '{TenantId}' not found", _options.DefaultTenantId);
                        if (_options.RequireTenant)
                        {
                            throw new TenantNotFoundException($"Default tenant '{_options.DefaultTenantId}' not found");
                        }
                    }
                }
                break;

            case TenantNotFoundAction.Continue:
                _logger.LogDebug("Continuing without tenant as configured");
                break;

            default:
                throw new InvalidOperationException($"Unknown tenant not found action: {_options.NotFoundAction}");
        }
    }

    private static string GetTenantId(TTenant tenant) =>
        tenant switch
        {
            ITenant t => t.Id,
            _ => tenant.ToString() ?? "Unknown"
        };
}