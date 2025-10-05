namespace Opplat.MultiTenant.Abstractions;

/// <summary>
/// Defines the contract for a tenant in a multi-tenant application.
/// </summary>
public interface ITenant
{
    /// <summary>
    /// Gets the unique identifier for the tenant.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the tenant.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets a value indicating whether the tenant is active.
    /// </summary>
    bool IsActive { get; }
}