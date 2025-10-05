using Microsoft.AspNetCore.Builder;
using Opplat.MultiTenant.Middleware;

namespace Opplat.MultiTenant.Extensions;

/// <summary>
/// Extension methods for configuring multi-tenant middleware in the application builder.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds multi-tenant middleware to the application pipeline.
    /// </summary>
    /// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This middleware should be added early in the pipeline, typically after authentication
    /// but before authorization and other tenant-aware middleware.
    /// </remarks>
    public static IApplicationBuilder UseMultiTenant<TTenant>(this IApplicationBuilder app)
        where TTenant : class
    {
        ArgumentNullException.ThrowIfNull(app);
        
        return app.UseMiddleware<TenantResolutionMiddleware<TTenant>>();
    }

    /// <summary>
    /// Adds multi-tenant middleware with the default Tenant type to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This middleware should be added early in the pipeline, typically after authentication
    /// but before authorization and other tenant-aware middleware.
    /// </remarks>
    public static IApplicationBuilder UseMultiTenant(this IApplicationBuilder app)
    {
        return app.UseMultiTenant<Models.Tenant>();
    }
}