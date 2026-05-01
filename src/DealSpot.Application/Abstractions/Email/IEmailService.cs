namespace DealSpot.Application.Abstractions.Email;

public interface IEmailService
{
    Task SendDealAlertAsync(string toEmail, string toName, DealAlertEmailModel model, CancellationToken cancellationToken = default);
    Task SendWatchlistMatchAsync(string toEmail, string toName, WatchlistMatchEmailModel model, CancellationToken cancellationToken = default);
}

public sealed record DealAlertEmailModel(
    string TenantName, string DealTitle, string DealUrl, string DealImageUrl,
    decimal CurrentPrice, decimal? OriginalPrice, string Currency, string CategoryName, string RetailerName);

public sealed record WatchlistMatchEmailModel(
    string TenantName, string WatchedKeyword, IReadOnlyList<DealAlertEmailModel> MatchingDeals);
