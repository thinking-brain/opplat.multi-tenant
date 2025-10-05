# Opplat.MultiTenant - Architecture Overview

## Initial Approach for Multi-Tenant Capabilities

This document outlines the comprehensive initial approach implemented for adding multi-tenant capabilities to any .NET application.

## Core Architecture Principles

### 1. **Flexibility First**
- **Generic Design**: Support for any tenant type through `TTenant` generic constraints
- **Pluggable Components**: Modular architecture with swappable implementations
- **Multiple Resolution Strategies**: Header, Query String, Subdomain, and custom resolvers

### 2. **Performance Optimized**
- **Async/Await**: All I/O operations are asynchronous with `ConfigureAwait(false)`
- **Caching Support**: Built-in caching mechanisms for tenant data
- **Efficient Data Structures**: Use of `ConcurrentDictionary` and optimized lookups

### 3. **Production Ready**
- **Comprehensive Error Handling**: Custom exceptions with meaningful context
- **Extensive Logging**: Detailed logging throughout the resolution pipeline
- **Configuration Driven**: Flexible options system for different scenarios

## Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                         │
├─────────────────────────────────────────────────────────────────┤
│  Controllers/Endpoints                                          │
│  ├─ ITenantContext<TTenant> (Dependency Injection)             │
│  └─ Access to CurrentTenant                                    │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Middleware Layer                         │
├─────────────────────────────────────────────────────────────────┤
│  TenantResolutionMiddleware<TTenant>                            │
│  ├─ Resolves tenant from request                               │
│  ├─ Sets tenant context                                        │
│  ├─ Handles errors and fallbacks                               │
│  └─ Continues pipeline execution                               │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Resolution Layer                           │
├─────────────────────────────────────────────────────────────────┤
│  ITenantResolver<TTenant> Implementations                      │
│  ├─ HeaderTenantResolver        (X-Tenant-ID header)           │
│  ├─ QueryStringTenantResolver   (?tenant=id parameter)         │
│  ├─ SubdomainTenantResolver     (tenant.domain.com)            │
│  └─ CompositeTenantResolver     (Multiple strategies)          │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Provider Layer                            │
├─────────────────────────────────────────────────────────────────┤
│  ITenantProvider<TTenant> Implementations                      │
│  ├─ InMemoryTenantProvider      (For testing/simple scenarios) │
│  ├─ DatabaseTenantProvider      (Entity Framework)             │
│  ├─ ApiTenantProvider           (External API)                 │
│  └─ CachedTenantProvider        (Caching wrapper)              │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Context Layer                            │
├─────────────────────────────────────────────────────────────────┤
│  TenantContext<TTenant> (Scoped Service)                       │
│  ├─ Stores current tenant for request scope                    │
│  ├─ Thread-safe access                                         │
│  └─ Injectable into any service                                │
└─────────────────────────────────────────────────────────────────┘
```

## Key Components Implemented

### 1. Core Abstractions (`/Abstractions`)

#### `ITenant`
- Base interface for tenant entities
- Defines minimum contract (Id, Name, IsActive)

#### `ITenantContext<TTenant>`
- Scoped service for accessing current tenant
- Thread-safe within request scope
- Methods: `CurrentTenant`, `HasTenant`, `SetTenant()`, `ClearTenant()`

#### `ITenantResolver<TTenant>`
- Contract for tenant resolution strategies
- Methods: `ResolveTenantAsync()`, `CanResolveAsync()`
- Supports cancellation tokens

#### `ITenantProvider<TTenant>`
- Contract for tenant data access
- Methods: `GetTenantAsync()`, `GetAllTenantsAsync()`, `TenantExistsAsync()`
- Database/API agnostic

### 2. Service Implementations (`/Services`)

#### `TenantContext<TTenant>`
- Scoped implementation of `ITenantContext<TTenant>`
- Stores tenant for request duration
- Provides null-safe access patterns

### 3. Tenant Resolvers (`/Resolvers`)

#### `HeaderTenantResolver<TTenant>`
- Resolves tenants from HTTP headers
- Configurable header name (default: "X-Tenant-ID")
- Includes comprehensive logging

#### `QueryStringTenantResolver<TTenant>`
- Resolves tenants from query parameters
- Configurable parameter name (default: "tenant")
- URL-safe tenant identification

#### `CompositeTenantResolver<TTenant>`
- Tries multiple resolvers in priority order
- Fault-tolerant (continues on resolver failures)
- Comprehensive error logging

### 4. Tenant Providers (`/Providers`)

#### `InMemoryTenantProvider<TTenant>`
- Thread-safe in-memory tenant storage
- Ideal for testing and simple scenarios
- CRUD operations support
- Case-sensitive/insensitive lookup

### 5. Middleware (`/Middleware`)

#### `TenantResolutionMiddleware<TTenant>`
- Early pipeline middleware for tenant resolution
- Configurable error handling strategies
- Sets response headers for debugging
- Integrates with logging framework

### 6. Configuration (`/Configuration`)

#### `MultiTenantOptions`
- Comprehensive configuration options
- Error handling strategies (`ThrowException`, `UseDefault`, `Continue`)
- Caching configuration
- Resolution strategy settings

#### `TenantNotFoundAction` Enum
- `ThrowException`: Strict tenant requirement
- `UseDefault`: Fallback to default tenant
- `Continue`: Proceed without tenant

### 7. Extensions (`/Extensions`)

#### `ServiceCollectionExtensions`
- Fluent builder pattern for DI registration
- `AddMultiTenant<TTenant>()` entry point
- `MultiTenantBuilder<TTenant>` for configuration chaining

#### `ApplicationBuilderExtensions`
- `UseMultiTenant<TTenant>()` middleware registration
- Pipeline integration helpers

### 8. Exception Types (`/Exceptions`)

#### `TenantNotFoundException`
- Thrown when tenant cannot be found
- Includes tenant ID context
- Multiple constructor overloads

#### `TenantResolutionException`
- Thrown when resolution process fails
- Wraps underlying exceptions
- Provides resolution context

## Usage Patterns

### 1. Basic Setup

```csharp
// Services registration
builder.Services.AddMultiTenant<Tenant>(options =>
{
    options.RequireTenant = true;
    options.TenantHeaderName = "X-Tenant-ID";
    options.NotFoundAction = TenantNotFoundAction.ThrowException;
})
.WithTenantProvider<InMemoryTenantProvider<Tenant>>()
.WithHeaderResolver();

// Middleware registration
app.UseMultiTenant<Tenant>();
```

### 2. Dependency Injection Usage

```csharp
public class OrderService
{
    private readonly ITenantContext<Tenant> _tenantContext;
    
    public OrderService(ITenantContext<Tenant> tenantContext)
    {
        _tenantContext = tenantContext;
    }
    
    public async Task<List<Order>> GetOrdersAsync()
    {
        var tenant = _tenantContext.CurrentTenant;
        // Tenant-specific logic
        return await GetOrdersForTenantAsync(tenant.Id);
    }
}
```

### 3. Multiple Resolution Strategies

```csharp
builder.Services.AddMultiTenant<Tenant>()
    .WithCompositeResolver(
        typeof(HeaderTenantResolver<Tenant>),      // Try header first
        typeof(QueryStringTenantResolver<Tenant>), // Then query string
        typeof(CustomTenantResolver<Tenant>)       // Finally custom logic
    );
```

## Extensibility Points

### 1. Custom Tenant Resolvers
Implement `ITenantResolver<TTenant>` for custom resolution logic:

```csharp
public class JwtTenantResolver<TTenant> : ITenantResolver<TTenant>
{
    public async Task<TTenant?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        // Extract tenant from JWT claims
        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim?.Value != null)
        {
            return await _tenantProvider.GetTenantAsync(tenantClaim.Value, cancellationToken);
        }
        return null;
    }
}
```

### 2. Custom Tenant Providers
Implement `ITenantProvider<TTenant>` for custom data sources:

```csharp
public class ApiTenantProvider<TTenant> : ITenantProvider<TTenant>
{
    public async Task<TTenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // Fetch from external API
        var response = await _httpClient.GetAsync($"/api/tenants/{tenantId}", cancellationToken);
        // Parse and return tenant
    }
}
```

### 3. Custom Tenant Types
Create custom tenant models implementing `ITenant`:

```csharp
public class CustomTenant : ITenant
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    
    // Custom properties
    public string DatabaseConnectionString { get; set; } = null!;
    public Dictionary<string, string> Settings { get; set; } = new();
    public DateTimeOffset SubscriptionExpiry { get; set; }
}
```

## Testing Strategy

The architecture supports comprehensive testing:

### 1. Unit Testing
- All interfaces are mockable
- Services have no static dependencies
- Clear separation of concerns

### 2. Integration Testing
- `InMemoryTenantProvider` for test scenarios
- Middleware testing with `TestServer`
- End-to-end resolution testing

### 3. Performance Testing
- Benchmarkable resolution strategies
- Caching effectiveness testing
- Concurrent tenant access testing

## Best Practices Implemented

### 1. .NET Conventions
- **Async/Await**: All I/O operations are async
- **ConfigureAwait(false)**: Used in library code
- **Nullable Reference Types**: Enabled throughout
- **XML Documentation**: Comprehensive API documentation

### 2. Dependency Injection
- **Service Lifetimes**: Appropriate scoping (Singleton, Scoped, Transient)
- **Interface Segregation**: Small, focused interfaces
- **Builder Pattern**: Fluent configuration API

### 3. Error Handling
- **Specific Exceptions**: Custom exception types with context
- **Graceful Degradation**: Configurable error handling strategies
- **Comprehensive Logging**: Detailed operation logging

### 4. Performance
- **Caching**: Built-in caching support
- **Efficient Collections**: `ConcurrentDictionary` for thread safety
- **Memory Management**: Proper disposal patterns

## Security Considerations

### 1. Input Validation
- Tenant ID validation and sanitization
- Protection against tenant ID manipulation
- Case-sensitivity configuration

### 2. Isolation
- Proper tenant context scoping
- No shared state between requests
- Thread-safe implementations

### 3. Auditing
- Comprehensive logging of tenant resolution
- Error tracking and monitoring
- Performance metrics collection

## Future Enhancements

### 1. Planned Features
- Entity Framework Core integration
- Redis caching provider
- Subdomain-based resolution
- Azure Key Vault configuration

### 2. Advanced Scenarios
- Multi-database per tenant
- Tenant-aware authorization policies
- Dynamic tenant provisioning
- Tenant migration tools

This architecture provides a solid foundation for multi-tenant applications in .NET, with flexibility, performance, and maintainability as core principles.