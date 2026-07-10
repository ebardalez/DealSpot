using DealSpot.Application;
using DealSpot.Application.Abstractions.Caching;
using DealSpot.Application.Push;
using DealSpot.Application.Users.ProvisionUser;
using DealSpot.Infrastructure;
using DealSpot.Infrastructure.Caching;
using DealSpot.Infrastructure.Push;
using DealSpot.Infrastructure.Tenancy;
using DealSpot.Web;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Serilog;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// ─── Logging ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ─── Blazor SSR ─────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Application + Infrastructure ───────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<DealSpot.Web.AdSenseOptions>(builder.Configuration.GetSection(DealSpot.Web.AdSenseOptions.SectionName));

// ─── Output Caching (.NET 10) ────────────────────────────────────────────────
builder.AddRedisOutputCache("redis");
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5)));

    options.AddPolicy("DealPage", policy =>
        policy.Expire(TimeSpan.FromMinutes(30))
              .SetVaryByRouteValue("slug")
              .Tag("deals"));

    options.AddPolicy("CategoryPage", policy =>
        policy.Expire(TimeSpan.FromMinutes(10))
              .SetVaryByRouteValue("categorySlug")
              .Tag("categories"));
});
builder.Services.AddSingleton<ICacheInvalidator, OutputCacheInvalidator>();

// ─── Authentication (Social Login) ──────────────────────────────────────────
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnSigningIn = async context =>
        {
            // ponytail: provision domain User inline on cookie sign-in, no separate callback endpoint
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var name = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? email ?? "User";
            var avatar = context.Principal?.FindFirstValue("urn:google:picture");

            if (!string.IsNullOrEmpty(email))
            {
                var mediator = context.HttpContext.RequestServices.GetRequiredService<ISender>();
                var result = await mediator.Send(new ProvisionUserCommand(email, name, avatar));

                if (result.IsSuccess)
                {
                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    identity.AddClaim(new Claim("app_user_id", result.Value.ToString()));
                }
            }
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture");
    });

builder.Services.AddAuthorization(options =>
{
    // ponytail: admin = configured email list, no Identity role seeding. Add roles when more admins needed.
    var adminEmails = builder.Configuration.GetSection("Auth:AdminEmails").Get<string[]>() ?? [];
    options.AddPolicy("Admin", policy =>
        policy.RequireAssertion(ctx =>
        {
            var email = ctx.User.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            return email is not null && adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
        }));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ─── Middleware Pipeline ─────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

// Tenant resolution must run before auth and routing
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseAntiforgery();

// Hangfire dashboard — admin only in production
app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthFilter()]
});

app.MapStaticAssets();
app.MapRazorComponents<DealSpot.Web.Components.App>()
    .AddInteractiveServerRenderMode();

// Auth endpoints
app.MapGet("/auth/login/{provider}", async (string provider, HttpContext ctx) =>
    await ctx.ChallengeAsync(provider, new() { RedirectUri = "/" }));

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("Cookies");
    return Results.Redirect("/");
});

// ─── Push notification API ──────────────────────────────────────────────────
app.MapGet("/api/push/vapid-key", (IOptions<VapidOptions> opts) =>
    Results.Json(new { key = opts.Value.PublicKey }));

app.MapPost("/api/push/subscribe", async (SubscribePushRequest req, ISender mediator) =>
{
    var result = await mediator.Send(new SubscribePushCommand(req.Endpoint, req.P256dh, req.Auth));
    return result.IsSuccess ? Results.Ok() : Results.BadRequest();
}).RequireAuthorization();

app.MapPost("/api/push/unsubscribe", async (UnsubscribePushRequest req, ISender mediator) =>
{
    var result = await mediator.Send(new UnsubscribePushCommand(req.Endpoint));
    return result.IsSuccess ? Results.Ok() : Results.BadRequest();
}).RequireAuthorization();

app.MapDefaultEndpoints(); // Aspire health + liveness endpoints
app.UseInfrastructure();

app.Run();

// ponytail: minimal request records, no separate file
internal sealed record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
internal sealed record UnsubscribePushRequest(string Endpoint);
