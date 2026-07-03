using System.Security.Claims;
using DealSpot.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;

namespace DealSpot.Infrastructure.Authentication;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // ponytail: custom claim name, set during OAuth callback provisioning
    private const string AppUserIdClaim = "app_user_id";

    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(AppUserIdClaim);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
