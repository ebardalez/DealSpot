using DealSpot.Application;
using DealSpot.Infrastructure;
using DealSpot.Infrastructure.Tenancy;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Logging ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ─── Blazor SSR ─────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─── Application + Infrastructure ───────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── Output Caching (.NET 10) ────────────────────────────────────────────────
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
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
    });

builder.Services.AddAuthorization();
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
app.UseStaticFiles();
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

app.MapHealthChecks("/health");
app.UseInfrastructure();

app.Run();
