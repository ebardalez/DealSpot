using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Users;

public sealed class User : Entity
{
    private User() { }

    private User(Guid id, string email, string displayName, string? avatarUrl) : base(id)
    {
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        ReputationScore = 0;
    }

    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public int ReputationScore { get; private set; }

    public static Result<User> Create(string email, string displayName, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>(UserErrors.InvalidEmail);
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<User>(UserErrors.InvalidDisplayName);

        return Result.Success(new User(Guid.NewGuid(), email.ToLowerInvariant(), displayName, avatarUrl));
    }

    public void UpdateProfile(string displayName, string? avatarUrl)
    {
        if (!string.IsNullOrWhiteSpace(displayName)) DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }

    public void AddReputation(int points) =>
        ReputationScore = Math.Max(0, ReputationScore + points);
}
