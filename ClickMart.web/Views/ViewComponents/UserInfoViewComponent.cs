using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.web.ViewComponents
{
    public class UserInfoViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // En ViewComponent, usa HttpContext.User y cástear a ClaimsPrincipal
            var user = HttpContext?.User as ClaimsPrincipal;

            var isAuth = user?.Identity?.IsAuthenticated == true;
            var name = isAuth ? (user?.Identity?.Name ?? "Usuario") : "Invitado";
            var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "-";

            return View((name, role));
        }
    }
}