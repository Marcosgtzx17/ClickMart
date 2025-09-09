using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
