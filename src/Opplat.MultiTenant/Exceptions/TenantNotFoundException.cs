namespace Opplat.MultiTenant.Exceptions;

/// <summary>
/// Exception thrown when a requested tenant cannot be found.
/// </summary>
public class TenantNotFoundException : Exception
{
    /// <summary>
    /// Gets the tenant identifier that was not found.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
    /// </summary>
    public TenantNotFoundException()
        : base("Tenant not found")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public TenantNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TenantNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was not found.</param>
    /// <param name="message">The exception message.</param>
    public TenantNotFoundException(string? tenantId, string message)
        : base(message)
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantNotFoundException"/> class.
    /// </summary>
    /// <param name="tenantId">The tenant identifier that was not found.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TenantNotFoundException(string? tenantId, string message, Exception innerException)
        : base(message, innerException)
    {
        TenantId = tenantId;
    }
}