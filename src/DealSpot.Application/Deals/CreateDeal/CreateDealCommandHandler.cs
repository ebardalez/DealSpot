using DealSpot.Application.Abstractions.Affiliate;
using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.ValueObjects;

namespace DealSpot.Application.Deals.CreateDeal;

public sealed record CreateDealCommand(
    Guid TenantId, Guid CategoryId, Guid? SubmittedByUserId,
    string Title, string Description,
    decimal CurrentPrice, decimal? OriginalPrice, string Currency,
    string ProductUrl, string? ImageUrl, string RetailerName) : ICommand<CreateDealResponse>;

public sealed record CreateDealResponse(Guid DealId, string Slug, string AffiliateUrl);

public sealed class CreateDealCommandHandler(
    IDealRepository dealRepository,
    ICategoryRepository categoryRepository,
    IAffiliateUrlRewriter affiliateUrlRewriter,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateDealCommand, CreateDealResponse>
{
    public async Task<Result<CreateDealResponse>> Handle(CreateDealCommand command, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null || category.TenantId != command.TenantId)
            return Result.Failure<CreateDealResponse>(CategoryErrors.NotFound);

        var rewriteResult = await affiliateUrlRewriter.RewriteAsync(command.TenantId, command.ProductUrl, cancellationToken);
        var slug = GenerateSlug(command.Title, command.CurrentPrice);
        var price = new DealPrice(command.CurrentPrice, command.OriginalPrice, command.Currency);

        var dealResult = Deal.Create(
            command.TenantId, command.CategoryId, command.SubmittedByUserId,
            command.Title, slug, command.Description, price,
            rewriteResult.CanonicalUrl, rewriteResult.AffiliateUrl,
            command.ImageUrl, command.RetailerName);

        if (dealResult.IsFailure)
            return Result.Failure<CreateDealResponse>(dealResult.Error);

        dealRepository.Add(dealResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDealResponse(dealResult.Value.Id, dealResult.Value.Slug, dealResult.Value.AffiliateUrl));
    }

    private static string GenerateSlug(string title, decimal price)
    {
        var slug = title.ToLowerInvariant().Replace(" ", "-").Replace("'", "").Replace("&", "and");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');
        return $"{slug}-{(int)price}";
    }
}
