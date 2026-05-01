using FluentValidation;

namespace DealSpot.Application.Deals.CreateDeal;

public sealed class CreateDealCommandValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.CurrentPrice).GreaterThan(0).WithMessage("Current price must be greater than zero.");
        RuleFor(x => x.OriginalPrice)
            .GreaterThan(x => x.CurrentPrice)
            .When(x => x.OriginalPrice.HasValue)
            .WithMessage("Original price must be greater than current price.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g. USD).");
        RuleFor(x => x.ProductUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Product URL must be a valid absolute URL.");
        RuleFor(x => x.RetailerName).NotEmpty().MaximumLength(100);
    }
}
