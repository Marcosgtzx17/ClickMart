using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.web.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private const string AuthScheme = "AuthCookie";
        private readonly AuthService _auth;

        public AuthController(AuthService auth) => _auth = auth;

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(UsuarioLoginDTO dto)
        {
            var res = await _auth.LoginAsync(dto);
            if (res is null || string.IsNullOrWhiteSpace(res.Token))
            {
                ViewBag.Error = "Credenciales inválidas";
                return View(dto);
            }
            var principal = ClaimsHelper.BuildPrincipalFromAuth(res);
            await HttpContext.SignInAsync(AuthScheme, principal);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Registrar() => View();

        [HttpPost]
        public async Task<IActionResult> Registrar(UsuarioRegistroDTO dto)
        {
            var res = await _auth.RegistrarAsync(dto);
            if (res is null || string.IsNullOrWhiteSpace(res.Token))
            {
                ViewBag.Error = "No se pudo registrar";
                return View(dto);
            }
            var principal = ClaimsHelper.BuildPrincipalFromAuth(res);
            await HttpContext.SignInAsync(AuthScheme, principal);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthScheme);
            return RedirectToAction("Login");
        }
    }
}