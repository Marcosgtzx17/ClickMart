using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly AuthService _auth;

        public UserController(AuthService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Tomamos el JWT guardado en los claims
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            try
            {
                var usuarios = await _auth.GetUsuariosAsync(token);
                return View(usuarios ?? new List<UsuarioRespuestaDTO>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"No se pudieron cargar los usuarios: {ex.Message}";
                return View(new List<UsuarioRespuestaDTO>());
            }
        }
    }
}