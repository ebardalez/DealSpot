using DealSpot.Domain.Abstractions;

namespace DealSpot.Domain.Users;

public static class UserErrors
{
    public static readonly Error NotFound = new("User.NotFound", "User was not found.");
    public static readonly Error InvalidEmail = new("User.InvalidEmail", "Email address cannot be empty.");
    public static readonly Error InvalidDisplayName = new("User.InvalidDisplayName", "Display name cannot be empty.");
    public static readonly Error EmailAlreadyInUse = new("User.EmailAlreadyInUse", "This email is already registered.");
}
