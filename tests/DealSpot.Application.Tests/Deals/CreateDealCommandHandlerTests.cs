using DealSpot.Application.Abstractions.Affiliate;
using DealSpot.Application.Deals.CreateDeal;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Categories;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals;

public sealed class CreateDealCommandHandlerTests
{
    private readonly IDealRepository _dealRepository = Substitute.For<IDealRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAffiliateUrlRewriter _rewriter = Substitute.For<IAffiliateUrlRewriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateDealCommandHandler _handler;

    public CreateDealCommandHandlerTests()
    {
        _handler = new CreateDealCommandHandler(
            _dealRepository, _categoryRepository, _userRepository, _rewriter, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateDealAndReturnResponse()
    {
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DealId.Should().NotBeEmpty();
        result.Value.AffiliateUrl.Should().Contain("tag=test-20");
        _dealRepository.Received(1).Add(Arg.Any<Deal>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnFailure()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var command = new CreateDealCommand(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Title", "Desc", 10m, null, "USD",
            "https://example.com", null, "Retailer");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToDifferentTenant_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var category = Category.Create(differentTenantId, "Electronics", "electronics").Value;

        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(category);

        var command = new CreateDealCommand(
            tenantId, Guid.NewGuid(), null,
            "Title", "Desc", 10m, null, "USD",
            "https://example.com", null, "Retailer");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
    }

    [Fact]
    public async Task Handle_TrustedUser_AutoApprovesSubmission()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var category = Category.Create(tenantId, "Headphones", "headphones").Value;
        var user = User.Create("trusted@test.com", "Trusted").Value;
        user.AddReputation(50); // meets CanAutoApprove threshold

        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(category);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _rewriter.RewriteAsync(tenantId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AffiliateRewriteResult("https://a.com?tag=t", "https://a.com", "a.com", true));

        var command = new CreateDealCommand(
            tenantId, category.Id, userId,
            "Great Deal", "Desc", 10m, 20m, "USD",
            "https://a.com", null, "Amazon",
            DealStatus.PendingApproval);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Deal should have been created as Active (auto-approved)
        _dealRepository.Received(1).Add(Arg.Is<Deal>(d => d.Status == DealStatus.Active));
    }
}
