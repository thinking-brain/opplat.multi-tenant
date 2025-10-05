using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Opplat.MultiTenant.Abstractions;
using Opplat.MultiTenant.Configuration;
using Opplat.MultiTenant.Services;
using Opplat.MultiTenant.Resolvers;

namespace Opplat.MultiTenant.Extensions;

/// <summary>
/// Extension methods for configuring multi-tenant services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds multi-tenant services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An optional action to configure multi-tenant options.</param>
    /// <returns>A <see cref="MultiTenantBuilder{TTenant}"/> for further configuration.</returns>
    public static MultiTenantBuilder<TTenant> AddMultiTenant<TTenant>(
        this IServiceCollection services,
        Action<MultiTenantOptions>? configure = null)
        where TTenant : class
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }

        // Register core services
        services.TryAddScoped<ITenantContext<TTenant>, TenantContext<TTenant>>();
        
        return new MultiTenantBuilder<TTenant>(services);
    }

    /// <summary>
    /// Adds multi-tenant services with a specific tenant type to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An optional action to configure multi-tenant options.</param>
    /// <returns>A <see cref="MultiTenantBuilder{Tenant}"/> for further configuration.</returns>
    public static MultiTenantBuilder<Models.Tenant> AddMultiTenant(
        this IServiceCollection services,
        Action<MultiTenantOptions>? configure = null)
    {
        return services.AddMultiTenant<Models.Tenant>(configure);
    }
}

/// <summary>
/// Builder for configuring multi-tenant services.
/// </summary>
/// <typeparam name="TTenant">The type representing a tenant in the application.</typeparam>
public class MultiTenantBuilder<TTenant> where TTenant : class
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenantBuilder{TTenant}"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public MultiTenantBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Adds a tenant resolver to the service collection.
    /// </summary>
    /// <typeparam name="TResolver">The type of the tenant resolver.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    public MultiTenantBuilder<TTenant> WithTenantResolver<TResolver>()
        where TResolver : class, ITenantResolver<TTenant>
    {
        Services.AddScoped<ITenantResolver<TTenant>, TResolver>();
        return this;
    }

    /// <summary>
    /// Adds a tenant provider to the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The type of the tenant provider.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    public MultiTenantBuilder<TTenant> WithTenantProvider<TProvider>()
        where TProvider : class, ITenantProvider<TTenant>
    {
        Services.AddScoped<ITenantProvider<TTenant>, TProvider>();
        return this;
    }

    /// <summary>
    /// Adds header-based tenant resolution.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public MultiTenantBuilder<TTenant> WithHeaderResolver()
    {
        return WithTenantResolver<HeaderTenantResolver<TTenant>>();
    }

    /// <summary>
    /// Adds query string-based tenant resolution.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public MultiTenantBuilder<TTenant> WithQueryStringResolver()
    {
        return WithTenantResolver<QueryStringTenantResolver<TTenant>>();
    }

    /// <summary>
    /// Adds composite tenant resolution that tries multiple resolvers in order.
    /// </summary>
    /// <param name="resolverTypes">The types of resolvers to use in priority order.</param>
    /// <returns>The builder for method chaining.</returns>
    public MultiTenantBuilder<TTenant> WithCompositeResolver(params Type[] resolverTypes)
    {
        foreach (var resolverType in resolverTypes)
        {
            if (!typeof(ITenantResolver<TTenant>).IsAssignableFrom(resolverType))
            {
                throw new ArgumentException($"Type {resolverType.Name} does not implement ITenantResolver<{typeof(TTenant).Name}>", nameof(resolverTypes));
            }
            Services.AddScoped(typeof(ITenantResolver<TTenant>), resolverType);
        }

        Services.AddScoped<ITenantResolver<TTenant>>(provider =>
        {
            var resolvers = provider.GetServices<ITenantResolver<TTenant>>()
                .Where(r => r.GetType() != typeof(CompositeTenantResolver<TTenant>));
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CompositeTenantResolver<TTenant>>>();
            return new CompositeTenantResolver<TTenant>(resolvers, logger);
        });

        return this;
    }

    /// <summary>
    /// Configures multi-tenant options.
    /// </summary>
    /// <param name="configure">The action to configure options.</param>
    /// <returns>The builder for method chaining.</returns>
    public MultiTenantBuilder<TTenant> WithConfiguration(Action<MultiTenantOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }
}