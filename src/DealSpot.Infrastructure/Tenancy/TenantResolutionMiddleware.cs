using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace DealSpot.Infrastructure.Tenancy;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantRepository tenantRepository)
    {
        var host = context.Request.Host.Host;

        var tenant = await cache.GetOrCreateAsync(
            $"tenant:{host}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await tenantRepository.GetByDomainAsync(host);
            });

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"No tenant found for domain: {host}");
            return;
        }

        if (tenantContext is TenantContext resolvedContext)
            resolvedContext.SetTenant(tenant);

        await next(context);
    }
}

public sealed class TenantContext : ITenantContext
{
    private Tenant? _tenant;

    public Tenant Current => _tenant ?? throw new InvalidOperationException(
        "Tenant has not been resolved. Ensure TenantResolutionMiddleware is registered.");

    public bool IsResolved => _tenant is not null;

    internal void SetTenant(Tenant tenant) => _tenant = tenant;
}

