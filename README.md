# Opplat.MultiTenant

A comprehensive .NET library that provides multi-tenant capabilities to any .NET application through dependency injection, middleware, and extensible tenant resolution strategies.

[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Opplat.MultiTenant.svg)](https://www.nuget.org/packages/Opplat.MultiTenant/)

## Features

- **🏢 Flexible Tenant Resolution**: Multiple strategies for identifying tenants (Header, Query String, Subdomain, Custom)
- **⚡ High Performance**: Optimized with caching and efficient data structures
- **🔧 Dependency Injection**: Seamless integration with .NET's built-in DI container
- **🎛️ Configurable**: Extensive configuration options for different scenarios
- **📦 Provider Pattern**: Pluggable tenant data providers (In-Memory, Database, API, Custom)
- **🛡️ Error Handling**: Comprehensive exception handling and fallback strategies
- **📝 Well Documented**: Extensive XML documentation and examples
- **🧪 Testable**: Designed with testing in mind, fully mockable interfaces

## Quick Start

### Installation

```bash
dotnet add package Opplat.MultiTenant
```

### Basic Usage

**1. Configure Services (Program.cs or Startup.cs)**

```csharp
using Opplat.MultiTenant.Extensions;
using Opplat.MultiTenant.Models;
using Opplat.MultiTenant.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add multi-tenant services
builder.Services.AddMultiTenant(options =>
{
    options.RequireTenant = true;
    options.TenantHeaderName = "X-Tenant-ID";
    options.DefaultTenantId = "default";
})
.WithTenantProvider<InMemoryTenantProvider<Tenant>>()
.WithHeaderResolver()
.WithQueryStringResolver();

var app = builder.Build();

// Add multi-tenant middleware (place early in pipeline)
app.UseMultiTenant<Tenant>();
```

**2. Access Current Tenant in Controllers/Endpoints**

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ITenantContext<Tenant> _tenantContext;

    public OrdersController(ITenantContext<Tenant> tenantContext)
    {
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public IActionResult GetOrders()
    {
        var currentTenant = _tenantContext.CurrentTenant;
        
        if (currentTenant == null)
        {
            return BadRequest("No tenant context available");
        }

        // Use tenant-specific logic
        var orders = GetOrdersForTenant(currentTenant.Id);
        return Ok(orders);
    }
}
```

**3. Minimal API Example**

```csharp
app.MapGet("/api/data", (ITenantContext<Tenant> tenantContext) =>
{
    var tenant = tenantContext.CurrentTenant;
    return new 
    { 
        TenantId = tenant?.Id ?? "No Tenant",
        TenantName = tenant?.Name ?? "Unknown",
        Data = $"Tenant-specific data for {tenant?.Name}"
    };
});
```

## Advanced Configuration

### Multiple Tenant Resolution Strategies

```csharp
builder.Services.AddMultiTenant<Tenant>()
    .WithCompositeResolver(
        typeof(HeaderTenantResolver<Tenant>),
        typeof(QueryStringTenantResolver<Tenant>),
        typeof(SubdomainTenantResolver<Tenant>)
    );
```

### Custom Tenant Provider

```csharp
public class DatabaseTenantProvider : ITenantProvider<Tenant>
{
    private readonly ApplicationDbContext _context;

    public DatabaseTenantProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TenantExistsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);
    }
}

// Register in DI
builder.Services.AddMultiTenant<Tenant>()
    .WithTenantProvider<DatabaseTenantProvider>();
```

### Configuration Options

```csharp
builder.Services.AddMultiTenant(options =>
{
    // Tenant resolution
    options.RequireTenant = true;
    options.TenantHeaderName = "X-Tenant-ID";
    options.TenantQueryParameterName = "tenant";
    options.SubdomainPosition = 0;
    
    // Caching
    options.EnableCaching = true;
    options.CacheTimeout = TimeSpan.FromMinutes(30);
    
    // Error handling
    options.NotFoundAction = TenantNotFoundAction.ThrowException;
    options.DefaultTenantId = "default";
    
    // Case sensitivity
    options.IgnoreCase = true;
});
```

## Tenant Resolution Strategies

### 1. Header-based Resolution

```http
GET /api/data
X-Tenant-ID: tenant1
```

### 2. Query String Resolution

```http
GET /api/data?tenant=tenant1
```

### 3. Subdomain Resolution

```http
GET https://tenant1.myapp.com/api/data
```

### 4. Custom Resolution

```csharp
public class JwtTenantResolver : ITenantResolver<Tenant>
{
    public async Task<Tenant?> ResolveTenantAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        // Extract tenant from JWT claims
        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim?.Value != null)
        {
            return await _tenantProvider.GetTenantAsync(tenantClaim.Value, cancellationToken);
        }
        return null;
    }

    public Task<bool> CanResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(context.User.Identity?.IsAuthenticated == true);
    }
}
```

## Testing

The library is designed with testing in mind. All interfaces are mockable:

```csharp
[Test]
public async Task OrderController_GetOrders_ReturnsTenantSpecificOrders()
{
    // Arrange
    var mockTenantContext = new Mock<ITenantContext<Tenant>>();
    var testTenant = new Tenant { Id = "test-tenant", Name = "Test Corp" };
    mockTenantContext.Setup(x => x.CurrentTenant).Returns(testTenant);
    mockTenantContext.Setup(x => x.HasTenant).Returns(true);

    var controller = new OrdersController(mockTenantContext.Object);

    // Act
    var result = await controller.GetOrders();

    // Assert
    Assert.IsType<OkObjectResult>(result);
    mockTenantContext.Verify(x => x.CurrentTenant, Times.Once);
}
```

## Error Handling

### Built-in Exception Types

- `TenantNotFoundException`: Thrown when a tenant cannot be found
- `TenantResolutionException`: Thrown when tenant resolution fails

### Configurable Error Handling

```csharp
options.NotFoundAction = TenantNotFoundAction.ThrowException; // Default
options.NotFoundAction = TenantNotFoundAction.UseDefault;     // Fallback to default tenant
options.NotFoundAction = TenantNotFoundAction.Continue;       // Continue without tenant
```

## Examples

Check out the `src/Opplat.MultiTenant.Example` project for a complete working example that demonstrates:

- Setting up multi-tenant services
- Multiple tenant resolution strategies
- Accessing tenant context in endpoints
- Sample tenant data management

### Running the Example

```bash
cd src/Opplat.MultiTenant.Example
dotnet run
```

Then test with:

```bash
# Using header
curl -H "X-Tenant-ID: tenant1" https://localhost:5001/weatherforecast

# Using query string
curl https://localhost:5001/weatherforecast?tenant=tenant2

# Get current tenant info
curl -H "X-Tenant-ID: tenant1" https://localhost:5001/tenant

# List all tenants
curl https://localhost:5001/tenants
```

## Architecture

The library follows a clean, modular architecture:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Middleware    │───▶│  Tenant Resolver │───▶│ Tenant Provider │
│                 │    │                  │    │                 │
│ - Resolution    │    │ - Header         │    │ - In-Memory     │
│ - Context       │    │ - Query String   │    │ - Database      │
│ - Error Handling│    │ - Subdomain      │    │ - API           │
└─────────────────┘    │ - Custom         │    │ - Custom        │
                       └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │ Tenant Context  │
                       │                 │
                       │ - Scoped        │
                       │ - Thread-Safe   │
                       │ - Accessible    │
                       └─────────────────┘
```

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [ ] **v1.1**: Entity Framework Core integration
- [ ] **v1.2**: Subdomain-based tenant resolution
- [ ] **v1.3**: Redis caching provider
- [ ] **v1.4**: Azure Key Vault configuration provider
- [ ] **v1.5**: Multi-database per tenant support
- [ ] **v2.0**: Tenant-aware authorization policies

## Support

- 📖 [Documentation](https://docs.opplat.dev/multi-tenant)
- 🐛 [Issue Tracker](https://github.com/thinking-brain/opplat.multi-tenant/issues)
- 💬 [Discussions](https://github.com/thinking-brain/opplat.multi-tenant/discussions)
- 📧 [Email Support](mailto:support@opplat.dev)

---

**Made with ❤️ by the Opplat team**