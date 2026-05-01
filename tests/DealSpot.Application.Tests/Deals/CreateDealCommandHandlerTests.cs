using DealSpot.Application.Abstractions.Affiliate;
using DealSpot.Application.Deals.CreateDeal;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using FluentAssertions;
using NSubstitute;

namespace DealSpot.Application.Tests.Deals;

public sealed class CreateDealCommandHandlerTests
{
    private readonly IDealRepository _dealRepository = Substitute.For<IDealRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IAffiliateUrlRewriter _rewriter = Substitute.For<IAffiliateUrlRewriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateDealCommandHandler _handler;

    public CreateDealCommandHandlerTests()
    {
        _handler = new CreateDealCommandHandler(
            _dealRepository, _categoryRepository, _rewriter, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateDealAndReturnResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = Category.Create(tenantId, "Headphones", "headphones").Value;

        _categoryRepository.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);
        _rewriter.RewriteAsync(tenantId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AffiliateRewriteResult(
                "https://amazon.com/dp/B123?tag=test-20",
                "https://amazon.com/dp/B123",
                "amazon.com", true));

        var command = new CreateDealCommand(
            tenantId, categoryId, null,
            "Sony WH-1000XM5", "Best noise-cancelling headphones",
            279.99m, 399.99m, "USD",
            "https://amazon.com/dp/B123", null, "Amazon");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DealId.Should().NotBeEmpty();
        result.Value.AffiliateUrl.Should().Contain("tag=test-20");
        _dealRepository.Received(1).Add(Arg.Any<Deal>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnFailure()
    {
        // Arrange
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var command = new CreateDealCommand(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Title", "Desc", 10m, null, "USD",
            "https://example.com", null, "Retailer");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
        _dealRepository.DidNotReceive().Add(Arg.Any<Deal>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToDifferentTenant_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var category = Category.Create(differentTenantId, "Electronics", "electronics").Value;

        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(category);

        var command = new CreateDealCommand(
            tenantId, Guid.NewGuid(), null,
            "Title", "Desc", 10m, null, "USD",
            "https://example.com", null, "Retailer");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
    }
}
