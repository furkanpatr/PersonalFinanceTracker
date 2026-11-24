using System.Security.Claims;

namespace PersonalFinanceTracker.Api.Helpers;

public static class UserIdHelper
{
    public static bool TryGetUserId(
    ClaimsPrincipal user,
    out int userId)
    {
        var userIdValue =
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdValue, out userId);
    }
}
