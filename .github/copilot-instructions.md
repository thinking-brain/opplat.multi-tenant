# GitHub Copilot Instructions for Opplat.MultiTenant

This document provides specific guidance for GitHub Copilot when working on the Opplat.MultiTenant .NET library - a NuGet package that enables multi-tenant capabilities for .NET applications.

## Project Overview

**Purpose**: A comprehensive .NET library that provides multi-tenant functionality to any .NET application through dependency injection, middleware, and extensible tenant resolution strategies.

**Target Framework**: .NET 9.0
**Package Type**: NuGet Library
**Architecture**: Modular, dependency injection-based, with extensible tenant resolution

## .NET Best Practices

### Code Style and Conventions

#### Naming Conventions
- Use **PascalCase** for public members, types, and namespaces
- Use **camelCase** for private fields and local variables
- Use **IPascalCase** for interfaces (prefix with 'I')
- Use **TPascalCase** for generic type parameters
- Prefer descriptive names over abbreviations
- Use suffixes like `Service`, `Provider`, `Factory`, `Builder` for appropriate patterns

```csharp
// ✅ Good
public interface ITenantResolver<TTenant> where TTenant : class
public class DatabaseTenantProvider : ITenantProvider<Tenant>
private readonly ITenantContextAccessor _tenantContextAccessor;

// ❌ Avoid
public interface TenantRes
public class DbTenProv
private readonly ITenantContextAccessor tca;
```

#### File Organization
- One public type per file
- File name should match the primary type name
- Use folder structure that mirrors namespace hierarchy
- Group related functionality in dedicated folders (Services, Models, Extensions, etc.)

### Nullable Reference Types

Always leverage nullable reference types (enabled in project):

```csharp
// ✅ Explicit nullability
public class TenantContext
{
    public string TenantId { get; init; } = null!;
    public string? DisplayName { get; init; }
    public Dictionary<string, object?> Properties { get; init; } = new();
}

// ✅ Null validation
public void SetTenant(Tenant? tenant)
{
    ArgumentNullException.ThrowIfNull(tenant);
    // Implementation
}
```

### Dependency Injection Patterns

#### Service Registration
Use extension methods for clean DI registration:

```csharp
// ✅ Extension method pattern
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMultiTenant<TTenant>(
        this IServiceCollection services,
        Action<MultiTenantBuilder<TTenant>>? configure = null)
        where TTenant : class
    {
        var builder = new MultiTenantBuilder<TTenant>(services);
        configure?.Invoke(builder);
        return services;
    }
}
```

#### Service Lifetimes
- **Singleton**: Configuration, factories, stateless services
- **Scoped**: Per-request services, tenant context
- **Transient**: Lightweight, stateless operations

```csharp
// ✅ Appropriate lifetimes
services.AddSingleton<ITenantConfigurationProvider, TenantConfigurationProvider>();
services.AddScoped<ITenantContext, TenantContext>();
services.AddTransient<ITenantValidator, TenantValidator>();
```

### Async/Await Best Practices

#### Async Method Naming
- Suffix async methods with `Async`
- Prefer `Task<T>` over `Task` when returning values
- Use `ValueTask<T>` for high-frequency, potentially synchronous operations

```csharp
// ✅ Proper async naming and signatures
public async Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
{
    // Implementation
}

public async ValueTask<bool> ValidateTenantAsync(string tenantId)
{
    // Implementation for potentially cached/sync operation
}
```

#### ConfigureAwait Usage
Use `ConfigureAwait(false)` in library code to avoid deadlocks:

```csharp
// ✅ Library async pattern
public async Task<Tenant?> GetTenantFromDatabaseAsync(string tenantId)
{
    var result = await _dbContext.Tenants
        .FirstOrDefaultAsync(t => t.Id == tenantId)
        .ConfigureAwait(false);
    
    return result;
}
```

### Generic Constraints and Design

#### Generic Type Constraints
Use appropriate constraints for tenant types:

```csharp
// ✅ Meaningful constraints
public interface ITenantService<TTenant> 
    where TTenant : class, ITenant
{
    Task<TTenant?> GetCurrentTenantAsync();
}

// ✅ Multiple constraints when needed
public class TenantCache<TTenant, TKey> 
    where TTenant : class, ITenant
    where TKey : notnull, IEquatable<TKey>
{
    // Implementation
}
```

### Error Handling and Validation

#### Exception Handling
- Use specific exception types
- Provide meaningful error messages
- Include relevant context in exceptions

```csharp
// ✅ Specific exceptions with context
public class TenantNotFoundException : Exception
{
    public string TenantId { get; }
    
    public TenantNotFoundException(string tenantId) 
        : base($"Tenant with ID '{tenantId}' was not found.")
    {
        TenantId = tenantId;
    }
}

// ✅ Guard clauses
public void ConfigureTenant(string tenantId, TenantConfiguration config)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
    ArgumentNullException.ThrowIfNull(config);
    
    // Implementation
}
```

### Performance Considerations

#### Memory Allocation
- Use `Span<T>` and `Memory<T>` for high-performance scenarios
- Implement `IDisposable`/`IAsyncDisposable` for resource management
- Consider object pooling for frequently created objects

```csharp
// ✅ Efficient string operations
public bool IsTenantIdValid(ReadOnlySpan<char> tenantId)
{
    return !tenantId.IsEmpty && tenantId.Length <= 50;
}

// ✅ Proper disposal pattern
public class TenantContextAccessor : ITenantContextAccessor, IDisposable
{
    private bool _disposed;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Cleanup
        }
        _disposed = true;
    }
}
```

#### Caching Strategies
Implement intelligent caching for tenant data:

```csharp
// ✅ Memory cache with proper eviction
public class CachedTenantProvider<TTenant> : ITenantProvider<TTenant>
    where TTenant : class
{
    private readonly IMemoryCache _cache;
    private readonly ITenantProvider<TTenant> _innerProvider;
    
    public async Task<TTenant?> GetTenantAsync(string tenantId)
    {
        if (_cache.TryGetValue($"tenant:{tenantId}", out TTenant? cached))
            return cached;
            
        var tenant = await _innerProvider.GetTenantAsync(tenantId).ConfigureAwait(false);
        
        if (tenant is not null)
        {
            _cache.Set($"tenant:{tenantId}", tenant, TimeSpan.FromMinutes(30));
        }
        
        return tenant;
    }
}
```

## Multi-Tenant Specific Patterns

### Tenant Resolution Strategies

#### Interface Design
Create flexible tenant resolution interfaces:

```csharp
// ✅ Extensible tenant resolution
public interface ITenantResolver<TTenant> where TTenant : class
{
    Task<TTenant?> ResolveTenantAsync(HttpContext context);
    Task<bool> CanResolveAsync(HttpContext context);
}

// ✅ Multiple resolution strategies
public class CompositeTenantResolver<TTenant> : ITenantResolver<TTenant>
    where TTenant : class
{
    private readonly IEnumerable<ITenantResolver<TTenant>> _resolvers;
    
    public async Task<TTenant?> ResolveTenantAsync(HttpContext context)
    {
        foreach (var resolver in _resolvers)
        {
            if (await resolver.CanResolveAsync(context))
            {
                var tenant = await resolver.ResolveTenantAsync(context);
                if (tenant is not null)
                    return tenant;
            }
        }
        return null;
    }
}
```

### Tenant Context Management

#### Scoped Context Pattern
Implement proper tenant context scoping:

```csharp
// ✅ Scoped tenant context
public interface ITenantContext<TTenant> where TTenant : class
{
    TTenant? CurrentTenant { get; }
    bool HasTenant { get; }
    void SetTenant(TTenant tenant);
}

public class TenantContext<TTenant> : ITenantContext<TTenant> 
    where TTenant : class
{
    private TTenant? _currentTenant;
    
    public TTenant? CurrentTenant => _currentTenant;
    public bool HasTenant => _currentTenant is not null;
    
    public void SetTenant(TTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        _currentTenant = tenant;
    }
}
```

### Middleware Implementation

#### Tenant Resolution Middleware
Create robust middleware for tenant resolution:

```csharp
// ✅ Proper middleware pattern
public class TenantResolutionMiddleware<TTenant>
    where TTenant : class
{
    private readonly RequestDelegate _next;
    private readonly ITenantResolver<TTenant> _tenantResolver;
    private readonly ILogger<TenantResolutionMiddleware<TTenant>> _logger;
    
    public TenantResolutionMiddleware(
        RequestDelegate next,
        ITenantResolver<TTenant> tenantResolver,
        ILogger<TenantResolutionMiddleware<TTenant>> logger)
    {
        _next = next;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var tenant = await _tenantResolver.ResolveTenantAsync(context);
        
        if (tenant is not null)
        {
            var tenantContext = context.RequestServices.GetRequiredService<ITenantContext<TTenant>>();
            tenantContext.SetTenant(tenant);
            
            _logger.LogDebug("Resolved tenant: {TenantId}", GetTenantId(tenant));
        }
        else
        {
            _logger.LogWarning("Could not resolve tenant for request: {Path}", context.Request.Path);
        }
        
        await _next(context);
    }
    
    private static string GetTenantId(TTenant tenant) =>
        tenant switch
        {
            ITenant t => t.Id,
            _ => tenant.ToString() ?? "Unknown"
        };
}
```

## NuGet Package Development Best Practices

### Project Configuration

#### Package Metadata
Ensure proper package metadata in .csproj:

```xml
<PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- Package Information -->
    <PackageId>Opplat.MultiTenant</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Authors>Your Name</Authors>
    <Description>A comprehensive multi-tenant library for .NET applications</Description>
    <PackageTags>multitenant;saas;dotnet;aspnetcore</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryUrl>https://github.com/yourusername/opplat.multi-tenant</RepositoryUrl>
    
    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

#### Multi-Targeting (Future Consideration)
Consider supporting multiple framework versions:

```xml
<!-- When ready for broader compatibility -->
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

### API Design Principles

#### Public API Surface
Design clean, intuitive public APIs:

```csharp
// ✅ Fluent builder pattern
public class MultiTenantBuilder<TTenant> where TTenant : class
{
    public MultiTenantBuilder<TTenant> WithTenantResolver<TResolver>()
        where TResolver : class, ITenantResolver<TTenant>
    {
        Services.AddScoped<ITenantResolver<TTenant>, TResolver>();
        return this;
    }
    
    public MultiTenantBuilder<TTenant> WithConfiguration(Action<MultiTenantOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }
}
```

#### Backward Compatibility
Design APIs with future extensibility in mind:

```csharp
// ✅ Extensible options pattern
public class MultiTenantOptions
{
    public bool RequireTenant { get; set; } = true;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public string DefaultTenantId { get; set; } = string.Empty;
    
    // Future extensibility
    public Dictionary<string, object> ExtendedProperties { get; set; } = new();
}
```

### Documentation Standards

#### XML Documentation
Provide comprehensive XML documentation:

```csharp
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
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the resolved tenant, or <see langword="null"/> if no tenant could be resolved.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    Task<TTenant?> ResolveTenantAsync(HttpContext context);
}
```

### Testing Patterns

#### Unit Testing
Write comprehensive unit tests with proper mocking:

```csharp
// ✅ Comprehensive test structure
public class TenantResolverTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ITenantProvider<Tenant>> _tenantProviderMock;
    private readonly HeaderTenantResolver<Tenant> _resolver;
    
    public TenantResolverTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _tenantProviderMock = new Mock<ITenantProvider<Tenant>>();
        _resolver = new HeaderTenantResolver<Tenant>(_tenantProviderMock.Object);
    }
    
    [Fact]
    public async Task ResolveTenantAsync_WithValidTenantHeader_ReturnsTenant()
    {
        // Arrange
        var tenantId = "test-tenant";
        var expectedTenant = new Tenant { Id = tenantId };
        var context = CreateHttpContext(headers: new() { ["X-Tenant-ID"] = tenantId });
        
        _tenantProviderMock
            .Setup(p => p.GetTenantAsync(tenantId))
            .ReturnsAsync(expectedTenant);
        
        // Act
        var result = await _resolver.ResolveTenantAsync(context);
        
        // Assert
        Assert.Equal(expectedTenant, result);
        _tenantProviderMock.Verify(p => p.GetTenantAsync(tenantId), Times.Once);
    }
}
```

## Code Quality and Maintenance

### Static Analysis
Enable appropriate analyzers and code quality tools:

```xml
<PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>NU1701</WarningsNotAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" PrivateAssets="all" />
</ItemGroup>
```

### Performance Testing
Include performance benchmarks for critical paths:

```csharp
// ✅ Benchmark critical operations
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TenantResolutionBenchmarks
{
    [Benchmark]
    public async Task ResolveTenantFromHeader()
    {
        // Benchmark tenant resolution performance
    }
}
```

## Common Anti-Patterns to Avoid

### ❌ Static State
```csharp
// ❌ Avoid static tenant state
public static class TenantManager
{
    public static Tenant? CurrentTenant { get; set; }  // Thread-unsafe, breaks multi-tenancy
}
```

### ❌ Tight Coupling
```csharp
// ❌ Avoid tight coupling to specific tenant types
public class OrderService
{
    public void ProcessOrder(CompanyTenant tenant)  // Coupled to specific tenant type
    {
        // Implementation
    }
}
```

### ❌ Synchronous Database Operations
```csharp
// ❌ Avoid blocking database calls
public Tenant GetTenant(string id)
{
    return _context.Tenants.FirstOrDefault(t => t.Id == id);  // Blocking
}
```

---

## Summary

When working on this multi-tenant library:

1. **Prioritize flexibility** - Use generics and interfaces to support various tenant types
2. **Embrace async/await** - All I/O operations should be asynchronous
3. **Design for DI** - Follow dependency injection patterns throughout
4. **Document extensively** - Provide clear XML documentation for all public APIs
5. **Test comprehensively** - Write unit tests, integration tests, and performance benchmarks
6. **Follow .NET conventions** - Adhere to established .NET naming and coding standards
7. **Consider performance** - Use appropriate caching and efficient data structures
8. **Plan for extensibility** - Design APIs that can evolve without breaking changes

The goal is to create a production-ready, high-performance, and developer-friendly multi-tenant library that integrates seamlessly with existing .NET applications.