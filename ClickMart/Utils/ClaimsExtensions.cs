using System.Security.Claims;

namespace ClickMart.Utils
{
    public static class ClaimsExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var raw = user.FindFirst("uid")?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.Identity?.Name;
            return int.TryParse(raw, out var id) ? id : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
    }
}
