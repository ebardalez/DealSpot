using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.GetDeals;

public sealed record GetDealsQuery(Guid TenantId, Guid? CategoryId, int Page, int PageSize) : IQuery<GetDealsResponse>;

public sealed record GetDealsResponse(IReadOnlyList<DealSummaryDto> Deals, int TotalCount, int Page, int PageSize);

public sealed record DealSummaryDto(
    Guid Id, string Title, string Slug, string Description,
    decimal CurrentPrice, decimal? OriginalPrice, decimal? DiscountPercentage, string Currency,
    string AffiliateUrl, string? ImageUrl, string RetailerName, string CategoryName, string? CategorySlug,
    int DealScore, string DealScoreLabel, int NetScore, DateTime CreatedAtUtc);

public sealed class GetDealsQueryHandler(
    IDealRepository dealRepository,
    ICategoryRepository categoryRepository) : IQueryHandler<GetDealsQuery, GetDealsResponse>
{
    public async Task<Result<GetDealsResponse>> Handle(GetDealsQuery query, CancellationToken cancellationToken)
    {
        var deals = query.CategoryId.HasValue
            ? await dealRepository.GetActiveByCategoryAsync(query.TenantId, query.CategoryId.Value, query.Page, query.PageSize, cancellationToken)
            : await dealRepository.GetActiveByTenantAsync(query.TenantId, query.Page, query.PageSize, cancellationToken);

        // ponytail: load all tenant categories once for name lookup, no join. Fine for <100 categories.
        var categories = await categoryRepository.GetByTenantAsync(query.TenantId, cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.Id, c => (c.Name, c.Slug));

        var dtos = deals.Select(d =>
        {
            categoryMap.TryGetValue(d.CategoryId, out var cat);
            return new DealSummaryDto(
                d.Id, d.Title, d.Slug, d.Description,
                d.Price.CurrentPrice, d.Price.OriginalPrice, d.Price.DiscountPercentage, d.Price.Currency,
                d.AffiliateUrl, d.ImageUrl, d.RetailerName, cat.Name ?? string.Empty, cat.Slug,
                d.DealScore.Value, d.DealScore.Label, d.NetScore, d.CreatedAtUtc);
        }).ToList();

        return Result.Success(new GetDealsResponse(dtos, dtos.Count, query.Page, query.PageSize));
    }
}
