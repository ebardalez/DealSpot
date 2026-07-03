using DealSpot.Application.Abstractions.Messaging;
using DealSpot.Domain.Abstractions;
using DealSpot.Domain.Users;

namespace DealSpot.Application.Users.ProvisionUser;

public sealed record ProvisionUserCommand(string Email, string DisplayName, string? AvatarUrl) : ICommand<Guid>;

public sealed class ProvisionUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ProvisionUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ProvisionUserCommand command, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByEmailAsync(command.Email.ToLowerInvariant(), cancellationToken);
        if (existing is not null)
        {
            existing.UpdateProfile(command.DisplayName, command.AvatarUrl);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var result = User.Create(command.Email, command.DisplayName, command.AvatarUrl);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        userRepository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.Id;
    }
}
