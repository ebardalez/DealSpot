using DealSpot.Application.Abstractions.Affiliate;
using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Clock;
using DealSpot.Application.Abstractions.Email;
using DealSpot.Application.Abstractions.Push;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Tenants;
using DealSpot.Domain.Users;
using DealSpot.Infrastructure.Affiliate;
using DealSpot.Infrastructure.Authentication;
using DealSpot.Infrastructure.BackgroundJobs;
using DealSpot.Infrastructure.Clock;
using DealSpot.Infrastructure.Email;
using DealSpot.Infrastructure.Persistence;
using DealSpot.Infrastructure.Persistence.Repositories;
using DealSpot.Infrastructure.Push;
using DealSpot.Infrastructure.Tenancy;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Resend;

namespace DealSpot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddBackgroundJobs(services, configuration);
        AddTenancy(services);
        AddAffiliate(services);
        AddEmail(services, configuration);
        AddPush(services, configuration);
        AddHttpClients(services);
        AddHealthChecks(services, configuration);
        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("dealspot") ?? configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IDealVoteRepository, DealVoteRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IPendingAlertRepository, PendingAlertRepository>();
        services.AddScoped<IPriceSnapshotRepository, PriceSnapshotRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
    }

    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("dealspot") ?? configuration.GetConnectionString("Database")!;

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues = ["default", "critical"];
        });
    }

    private static void AddTenancy(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantContext>(sp => (TenantContext)sp.GetRequiredService<ITenantContext>());
        services.AddScoped<ICurrentUser, CurrentUser>();
    }

    private static void AddAffiliate(IServiceCollection services) =>
        services.AddScoped<IAffiliateUrlRewriter, AffiliateUrlRewriter>();

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
            o.ApiToken = configuration["Email:ResendApiKey"] ?? string.Empty);
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailService, ResendEmailService>();
    }

    private static void AddPush(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VapidOptions>(configuration.GetSection(VapidOptions.SectionName));
        services.AddScoped<IPushSender, WebPushSender>();
    }

    private static void AddHttpClients(IServiceCollection services) =>
        services.AddHttpClient("DealHealthCheck", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "DealSpot-HealthCheck/1.0");
        });

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration) =>
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("dealspot") ?? configuration.GetConnectionString("Database")!);

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            // ponytail: auto-migrate on startup. Aspire ensures DB container is ready (WaitFor).
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed");
                throw;
            }

            // ponytail: seed dev data after migration. Separate try/catch so seed failure doesn't crash app.
            if (app.Environment.IsDevelopment())
            {
                try
                {
                    SeedDevelopmentData.SeedAsync(db, logger).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Development seed data failed — app continues without seed data");
                }
            }
        }

        RecurringJob.AddOrUpdate<DealUrlHealthCheckJob>(
            "deal-url-health-check",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 */6 * * *");

        RecurringJob.AddOrUpdate<DailyDigestJob>(
            "daily-digest",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(8));

        RecurringJob.AddOrUpdate<DealScoreRecalculationJob>(
            "deal-score-recalculation",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 */2 * * *");

        return app;
    }
}
