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

public sealed class UpdateReputationOnExpireHandlerTests
{
    private readonly IDealRepository _dealRepo = Substitute.For<IDealRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateReputationOnExpireHandler _handler;

    public UpdateReputationOnExpireHandlerTests()
    {
        _handler = new UpdateReputationOnExpireHandler(
            _dealRepo, _userRepo, _unitOfWork,
            Substitute.For<ILogger<UpdateReputationOnExpireHandler>>());
    }

    [Fact]
    public async Task Handle_CommunityReported_DeductsMinus3()
    {
        var submitterId = Guid.NewGuid();
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), submitterId,
            "Deal", "deal-10", "Desc",
            new DealPrice(10m, null, "USD"),
            "https://ex.com", "https://ex.com?tag=t",
            null, "Amazon").Value;

        var user = User.Create("sub@test.com", "Sub").Value;
        user.AddReputation(10);

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _userRepo.GetByIdAsync(submitterId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(
            new DealExpiredDomainEvent(deal.Id, deal.TenantId, DealExpiredReason.CommunityReported),
            CancellationToken.None);

        user.ReputationScore.Should().Be(7);
    }

    [Fact]
    public async Task Handle_UrlDead_DeductsMinus3()
    {
        var submitterId = Guid.NewGuid();
        var deal = Deal.Create(
            Guid.NewGuid(), Guid.NewGuid(), submitterId,
            "Deal", "deal-10", "Desc",
            new DealPrice(10m, null, "USD"),
            "https://ex.com", "https://ex.com?tag=t",
            null, "Amazon").Value;

        var user = User.Create("sub@test.com", "Sub").Value;
        user.AddReputation(5);

        _dealRepo.GetByIdAsync(deal.Id, Arg.Any<CancellationToken>()).Returns(deal);
        _userRepo.GetByIdAsync(submitterId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(
            new DealExpiredDomainEvent(deal.Id, deal.TenantId, DealExpiredReason.UrlDead),
            CancellationToken.None);

        user.ReputationScore.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ManualExpire_Skips()
    {
        await _handler.Handle(
            new DealExpiredDomainEvent(Guid.NewGuid(), Guid.NewGuid(), DealExpiredReason.Manual),
            CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScheduledExpire_Skips()
    {
        await _handler.Handle(
            new DealExpiredDomainEvent(Guid.NewGuid(), Guid.NewGuid(), DealExpiredReason.Scheduled),
            CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
