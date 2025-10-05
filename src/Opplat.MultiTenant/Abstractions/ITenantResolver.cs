using Microsoft.AspNetCore.Http;

namespace Opplat.MultiTenant.Abstractions;

/// <summary>
/// Provides tenant resolution capabilities for multi-tenant applications.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
/// <remarks>
/// This interface defines the contract for resolving tenants from HTTP requests.
/// Implementations should be registered as scoped services in the DI container.
/// </remarks>
public interface ITenantResolver<TTenant> where TTenant : class
{
    /// <summary>
    /// Resolves a tenant from the provided HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context containing request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the resolved tenant, or <see langword="null"/> if no tenant could be resolved.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    Task<TTenant?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether this resolver can resolve a tenant from the provided HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// <see langword="true"/> if this resolver can handle the request; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> CanResolveAsync(HttpContext context, CancellationToken cancellationToken = default);
}