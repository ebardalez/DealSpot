using DealSpot.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace DealSpot.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string ResendApiKey { get; init; } = string.Empty;
}

public sealed class ResendEmailService(
    IResend resendClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendDealAlertAsync(string toEmail, string toName, DealAlertEmailModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new EmailMessage
            {
                From = $"{_options.FromName} <{_options.FromAddress}>",
                To = [toEmail],
                Subject = $"🔥 New Deal: {model.DealTitle} – {model.CurrentPrice:C}",
                HtmlBody = $"<h2>{model.DealTitle}</h2><p><strong>{model.CurrentPrice:C}</strong></p><a href='{model.DealUrl}'>Get This Deal →</a>"
            };
            await resendClient.EmailSendAsync(message, cancellationToken);
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to send deal alert to {Email}", toEmail); throw; }
    }

    public async Task SendWatchlistMatchAsync(string toEmail, string toName, WatchlistMatchEmailModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var deals = string.Join("", model.MatchingDeals.Select(d => $"<li><a href='{d.DealUrl}'>{d.DealTitle}</a> – {d.CurrentPrice:C}</li>"));
            var message = new EmailMessage
            {
                From = $"{_options.FromName} <{_options.FromAddress}>",
                To = [toEmail],
                Subject = $"🎯 Watchlist Match: {model.MatchingDeals.Count} deal(s) for \"{model.WatchedKeyword}\"",
                HtmlBody = $"<h2>Watchlist Matches for \"{model.WatchedKeyword}\"</h2><ul>{deals}</ul>"
            };
            await resendClient.EmailSendAsync(message, cancellationToken);
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to send watchlist email to {Email}", toEmail); throw; }
    }

    public async Task SendDigestAsync(string toEmail, string toName, DigestEmailModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var deals = string.Join("", model.Deals.Select(d => $"<li><a href='{d.DealUrl}'>{d.DealTitle}</a> – {d.CurrentPrice:C} at {d.RetailerName}</li>"));
            var message = new EmailMessage
            {
                From = $"{_options.FromName} <{_options.FromAddress}>",
                To = [toEmail],
                Subject = $"📬 Your Daily Deal Digest — {model.Deals.Count} deal(s) on {model.TenantName}",
                HtmlBody = $"<h2>Your Daily Deal Digest</h2><p>{model.Deals.Count} new deal(s) matched your watchlist:</p><ul>{deals}</ul>"
            };
            await resendClient.EmailSendAsync(message, cancellationToken);
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to send digest to {Email}", toEmail); throw; }
    }
}
