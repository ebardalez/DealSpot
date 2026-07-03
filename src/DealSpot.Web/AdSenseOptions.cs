namespace DealSpot.Web;

// ponytail: plain POCO bound from config. No IOptions wrapper needed — inject directly via IConfiguration or bind once.
public sealed class AdSenseOptions
{
    public const string SectionName = "AdSense";

    public string ClientId { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string SlotDealDetail { get; init; } = string.Empty;
    public string SlotFeedInline { get; init; } = string.Empty;
    public string SlotSidebar { get; init; } = string.Empty;
}
