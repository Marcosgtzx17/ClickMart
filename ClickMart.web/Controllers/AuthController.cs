using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.web.Controllers
{
    public class AuthController : Controller
    {
        private const string AuthScheme = "AuthCookie";
        private readonly AuthService _auth;

        public AuthController(AuthService auth) => _auth = auth;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Denied() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UsuarioLoginDTO dto)
        {
            var res = await _auth.LoginAsync(dto);
            if (res is null || string.IsNullOrWhiteSpace(res.Token))
            {
                ViewBag.Error = "Credenciales inválidas";
                return View(dto);
            }

            var principal = ClaimsHelper.BuildPrincipalFromAuth(res);

            await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Registrar() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Registrar(UsuarioRegistroDTO dto)
        {
            var res = await _auth.RegistrarAsync(dto);
            if (res is null || string.IsNullOrWhiteSpace(res.Token))
            {
                ViewBag.Error = "No se pudo registrar";
                return View(dto);
            }

            var principal = ClaimsHelper.BuildPrincipalFromAuth(res);

            await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
