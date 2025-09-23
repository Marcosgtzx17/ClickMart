using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.web.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly UsuarioService _svc;
        public UsuarioController(UsuarioService svc) => _svc = svc;

        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            try
            {
                var list = await _svc.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<UsuarioListadoDTO>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpGet]
        public IActionResult Create() => View(new UsuarioCreateDTO());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return View(dto);

            try
            {
                var created = await _svc.CreateAsync(dto, token);
                if (created is null)
                {
                    ViewBag.Error = "No se pudo crear el usuario.";
                    return View(dto);
                }
                TempData["Ok"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            var u = await _svc.GetByIdAsync(id, token);
            if (u is null) return NotFound();

            var vm = new UsuarioUpdateDTO
            {
                Nombre = u.Nombre,
                Email = u.Email,
                Direccion = string.Empty, // rellena si tu API lo devuelve
                Telefono = string.Empty,  // rellena si tu API lo devuelve
                RolId = 0                 // si no conoces el rol, ajústalo aquí
            };
            ViewBag.Id = id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioUpdateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) { ViewBag.Id = id; return View(dto); }

            try
            {
                var updated = await _svc.UpdateAsync(id, dto, token);
                if (updated is null)
                {
                    ViewBag.Error = "No se pudo actualizar el usuario.";
                    ViewBag.Id = id;
                    return View(dto);
                }
                TempData["Ok"] = "Usuario actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Id = id;
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
            try
            {
                var ok = await _svc.DeleteAsync(id, token);
                if (!ok) TempData["Error"] = "No se pudo eliminar el usuario.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
