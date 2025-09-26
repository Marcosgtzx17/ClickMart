
using System.Security.Claims;

namespace ClickMart.Utils
{
    public static class ApiClaimsHelper
    {
        public static string? GetEmail(this ClaimsPrincipal user)
        {
          
            return user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value
                ?? user.FindFirst("preferred_username")?.Value   // AzureAD/OIDC
                ?? user.FindFirst("upn")?.Value                  // algunos IdPs
                ?? user.FindFirst("sub")?.Value                  // fallback
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
