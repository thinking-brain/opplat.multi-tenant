
using Opplat.MultiTenant.Abstractions;

namespace Opplat.MultiTenant.Models;

/// <summary>
/// Represents a tenant in a multi-tenant application.
/// </summary>
public class Tenant : ITenant
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display name of the tenant.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the connection string for the tenant's database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets additional properties for the tenant.
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date when the tenant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
