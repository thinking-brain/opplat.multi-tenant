using Opplat.MultiTenant.Extensions;
using Opplat.MultiTenant.Models;
using Opplat.MultiTenant.Providers;
using Opplat.MultiTenant.Abstractions;
using Opplat.MultiTenant.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure multi-tenant services
builder.Services.AddMultiTenant(options =>
{
    options.RequireTenant = false; // Allow requests without tenants for demo
    options.TenantHeaderName = "X-Tenant-ID";
    options.NotFoundAction = TenantNotFoundAction.Continue;
})
.WithTenantProvider<InMemoryTenantProvider<Tenant>>()
.WithHeaderResolver()
.WithQueryStringResolver();

// Create some sample tenants
builder.Services.AddSingleton<IEnumerable<Tenant>>(provider =>
{
    return new List<Tenant>
    {
        new() { Id = "tenant1", Name = "Acme Corporation", IsActive = true },
        new() { Id = "tenant2", Name = "Global Industries", IsActive = true },
        new() { Id = "tenant3", Name = "Tech Solutions", IsActive = true }
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add multi-tenant middleware
app.UseMultiTenant<Tenant>();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ITenantContext<Tenant> tenantContext) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)],
            tenantContext.CurrentTenant?.Name ?? "No Tenant"
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/tenant", (ITenantContext<Tenant> tenantContext) =>
{
    if (tenantContext.HasTenant)
    {
        return Results.Ok(new
        {
            TenantId = tenantContext.CurrentTenant!.Id,
            TenantName = tenantContext.CurrentTenant.Name,
            IsActive = tenantContext.CurrentTenant.IsActive,
            HasTenant = tenantContext.HasTenant
        });
    }

    return Results.Ok(new { Message = "No tenant resolved", HasTenant = false });
})
.WithName("GetCurrentTenant");

app.MapGet("/tenants", async (ITenantProvider<Tenant> tenantProvider) =>
{
    var tenants = await tenantProvider.GetAllTenantsAsync();
    return Results.Ok(tenants.Select(t => new
    {
        Id = t.Id,
        Name = t.Name,
        IsActive = t.IsActive
    }));
})
.WithName("GetAllTenants");

await app.RunAsync();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary, string TenantName)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
