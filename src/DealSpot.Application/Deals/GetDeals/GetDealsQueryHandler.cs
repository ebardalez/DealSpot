using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;

namespace DealSpot.Application.Deals.GetDeals;

public sealed record GetDealsQuery(Guid TenantId, Guid? CategoryId, int Page, int PageSize) : IQuery<GetDealsResponse>;

public sealed record GetDealsResponse(IReadOnlyList<DealSummaryDto> Deals, int TotalCount, int Page, int PageSize);

public sealed record DealSummaryDto(
    Guid Id, string Title, string Slug, string Description,
    decimal CurrentPrice, decimal? OriginalPrice, decimal? DiscountPercentage, string Currency,
    string AffiliateUrl, string? ImageUrl, string RetailerName, string CategoryName,
    int DealScore, string DealScoreLabel, DateTime CreatedAtUtc);

public sealed class GetDealsQueryHandler(IDealRepository dealRepository) : IQueryHandler<GetDealsQuery, GetDealsResponse>
{
    public async Task<Result<GetDealsResponse>> Handle(GetDealsQuery query, CancellationToken cancellationToken)
    {
        var deals = query.CategoryId.HasValue
            ? await dealRepository.GetActiveByCategoryAsync(query.TenantId, query.CategoryId.Value, query.Page, query.PageSize, cancellationToken)
            : await dealRepository.GetActiveByTenantAsync(query.TenantId, query.Page, query.PageSize, cancellationToken);

        var dtos = deals.Select(d => new DealSummaryDto(
            d.Id, d.Title, d.Slug, d.Description,
            d.Price.CurrentPrice, d.Price.OriginalPrice, d.Price.DiscountPercentage, d.Price.Currency,
            d.AffiliateUrl, d.ImageUrl, d.RetailerName, string.Empty,
            d.DealScore.Value, d.DealScore.Label, d.CreatedAtUtc)).ToList();

        return Result.Success(new GetDealsResponse(dtos, dtos.Count, query.Page, query.PageSize));
    }
}
