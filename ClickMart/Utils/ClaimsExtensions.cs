using System.Security.Claims;

namespace ClickMart.Utils
{
    public static class ClaimsExtensions
    {
        private static readonly string[] RoleClaimTypes = { ClaimTypes.Role, "role", "roles", "rol" };
        private static readonly HashSet<string> AdminAliases =
            new(new[] { "admin", "administrador", "administrator" }, StringComparer.OrdinalIgnoreCase);

        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var raw = user.FindFirst("uid")?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value
                     ?? user.Identity?.Name;

            return int.TryParse(raw, out var id) ? id : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            var roles = user.Claims
                .Where(c => RoleClaimTypes.Contains(c.Type))
                .SelectMany(c => (c.Value ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(v => v.Trim());

            return roles.Any(r => AdminAliases.Contains(r));
        }

        public static string? GetEmail(this ClaimsPrincipal user)
        {
            // Revisa varias claves típicas
            var email = user.FindFirst(ClaimTypes.Email)?.Value
                     ?? user.FindFirst("email")?.Value
                     ?? user.FindFirst("upn")?.Value
                     ?? user.Identity?.Name;

            // sanity check mínimo
            return string.IsNullOrWhiteSpace(email) ? null : email;
        }
    }
}
