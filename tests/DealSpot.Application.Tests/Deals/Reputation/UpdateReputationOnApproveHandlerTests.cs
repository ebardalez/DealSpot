using DealSpot.Application.Deals.Reputation;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Deals;
using DealSpot.Domain.Deals.Events;
using DealSpot.Domain.Deals.ValueObjects;
using DealSpot.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Deals.Reputation;

public sealed class UpdateReputationOnApproveHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateReputationOnApproveHandler _handler;

    public UpdateReputationOnApproveHandlerTests()
    {
        _handler = new UpdateReputationOnApproveHandler(
            _dealRepo, _userRepo, _unitOfWork,
            Substitute.For<ILogger<UpdateReputationOnApproveHandler>>());
    }

    [Fact]
    public async Task Handle_ApprovedDealWithSubmitter_GivesPlus10()
    {
        var submitterId = Guid.NewGuid();
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), submitterId,
            "Deal", "deal-10", "Desc",
            new DealPrice(10m, null, "USD"),
            "https://ex.com", "https://ex.com?tag=t",
            null, "Amazon",
            DealStatus.PendingApproval).Value;
        deal.Approve(); // triggers DealCreatedDomainEvent

        var user = User.Create("sub@test.com", "Sub").Value;

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _userRepo.GetByIdAsync(submitterId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new DealCreatedDomainEvent(deal.Id, deal.TenantId), CancellationToken.None);

        user.ReputationScore.Should().Be(10);
        _userRepo.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DealWithNoSubmitter_Skips()
    {
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "Deal", "deal-10", "Desc",
            new DealPrice(10m, null, "USD"),
            "https://ex.com", "https://ex.com?tag=t",
            null, "Amazon").Value;

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);

        await _handler.Handle(new DealCreatedDomainEvent(deal.Id, deal.TenantId), CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
