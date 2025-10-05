namespace Opplat.MultiTenant.Exceptions;

/// <summary>
/// Exception thrown when tenant resolution fails.
/// </summary>
public class TenantResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionException"/> class.
    /// </summary>
    public TenantResolutionException()
        : base("Tenant resolution failed")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public TenantResolutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TenantResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}