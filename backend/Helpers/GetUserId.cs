using System.Security.Claims;

namespace backend.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetCurrentUserId(this ClaimsPrincipal user)
        {
            var userIdString = user.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdString) ||
                !int.TryParse(userIdString, out int userId))
            {
                return null;
            }

            return userId;
        }
    }
}