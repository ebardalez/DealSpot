using DealSpot.Application.Users.ProvisionUser;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DealSpot.Application.Tests.Users;

public sealed class ProvisionUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProvisionUserCommandHandler _handler;

    public ProvisionUserCommandHandlerTests()
    {
        _handler = new ProvisionUserCommandHandler(_userRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndReturnsId()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new ProvisionUserCommand("alice@example.com", "Alice", "https://img.example.com/alice.jpg");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _userRepository.Received(1).Add(Arg.Any<User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingEmail_ReturnsSameIdWithoutAdd()
    {
        var existing = User.Create("alice@example.com", "Alice").Value;
        _userRepository.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new ProvisionUserCommand("Alice@Example.com", "Alice Updated", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsFailure()
    {
        var command = new ProvisionUserCommand("", "Name", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }
}
