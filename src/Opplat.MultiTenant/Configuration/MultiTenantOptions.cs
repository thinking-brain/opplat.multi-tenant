namespace Opplat.MultiTenant.Configuration;

/// <summary>
/// Configuration options for multi-tenant functionality.
/// </summary>
public class MultiTenantOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether a tenant is required for all requests.
    /// Default is true.
    /// </summary>
    public bool RequireTenant { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the HTTP header used for tenant identification.
    /// Default is "X-Tenant-ID".
    /// </summary>
    public string TenantHeaderName { get; set; } = "X-Tenant-ID";

    /// <summary>
    /// Gets or sets the query parameter name used for tenant identification.
    /// Default is "tenant".
    /// </summary>
    public string TenantQueryParameterName { get; set; } = "tenant";

    /// <summary>
    /// Gets or sets the subdomain position for tenant identification (0-based).
    /// Default is 0 (first subdomain).
    /// </summary>
    public int SubdomainPosition { get; set; } = 0;

    /// <summary>
    /// Gets or sets the cache timeout for tenant data.
    /// Default is 30 minutes.
    /// </summary>
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the default tenant identifier when no tenant can be resolved.
    /// </summary>
    public string? DefaultTenantId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tenant caching is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets additional properties for extending configuration.
    /// </summary>
    public Dictionary<string, object> ExtendedProperties { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether tenant resolution should ignore case.
    /// Default is true.
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets the action to take when a tenant is not found.
    /// </summary>
    public TenantNotFoundAction NotFoundAction { get; set; } = TenantNotFoundAction.ThrowException;
}

/// <summary>
/// Defines actions to take when a tenant is not found.
/// </summary>
public enum TenantNotFoundAction
{
    /// <summary>
    /// Throw an exception when tenant is not found.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Use the default tenant when tenant is not found.
    /// </summary>
    UseDefault,

    /// <summary>
    /// Continue processing without a tenant.
    /// </summary>
    Continue
}