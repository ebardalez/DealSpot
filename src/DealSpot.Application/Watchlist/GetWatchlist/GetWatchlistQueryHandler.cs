using DealSpot.Application.Abstractions.Authentication;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Application.Abstractions.Tenancy;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Users;

namespace DealSpot.Application.Watchlist.GetWatchlist;

public sealed record GetWatchlistQuery : IQuery<IReadOnlyList<WatchlistItemDto>>;

public sealed record WatchlistItemDto(
    Guid Id, string Type, string? CategoryName, string? CategorySlug,
    string? Keyword, decimal? TargetPrice, DateTime CreatedAtUtc);

public sealed class GetWatchlistQueryHandler(
    IWatchlistRepository watchlistRepository,
    ICategoryRepository categoryRepository,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : IQueryHandler<GetWatchlistQuery, IReadOnlyList<WatchlistItemDto>>
{
    public async Task<Result<IReadOnlyList<WatchlistItemDto>>> Handle(GetWatchlistQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var tenantId = tenantContext.Current.Id;

        var entries = await watchlistRepository.GetByUserAsync(userId, tenantId, cancellationToken);

        // ponytail: load categories once for name lookup, same pattern as GetDealsQueryHandler
        var categories = await categoryRepository.GetByTenantAsync(tenantId, cancellationToken);
        var catMap = categories.ToDictionary(c => c.Id, c => (c.Name, c.Slug));

        var dtos = entries.Select(e =>
        {
            var catName = e.CategoryId.HasValue && catMap.TryGetValue(e.CategoryId.Value, out var cat) ? cat.Name : null;
            var catSlug = e.CategoryId.HasValue && catMap.TryGetValue(e.CategoryId.Value, out var cat2) ? cat2.Slug : null;
            return new WatchlistItemDto(
                e.Id,
                e.CategoryId.HasValue ? "Category" : "Keyword",
                catName, catSlug,
                e.ProductKeyword, e.TargetPrice, e.CreatedAtUtc);
        }).ToList();

        return Result.Success<IReadOnlyList<WatchlistItemDto>>(dtos);
    }
}
