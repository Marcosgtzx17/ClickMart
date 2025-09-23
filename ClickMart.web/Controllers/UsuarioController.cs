using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.DTOs.RolDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;                  // UsuarioService, RolService, ApiHttpException
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;      // SelectListItem
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClickMart.web.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly UsuarioService _svc;
        private readonly RolService _roles;

        public UsuarioController(UsuarioService svc, RolService roles)
        {
            _svc = svc;
            _roles = roles;
        }

        // ------------------------------
        // Helpers
        // ------------------------------
        private static string? GetPropString(object obj, string name)
            => obj?.GetType().GetProperty(name)?.GetValue(obj)?.ToString();

        private static int? GetPropInt(object obj, string name)
        {
            var p = obj?.GetType().GetProperty(name);
            if (p == null) return null;
            var v = p.GetValue(obj);
            if (v == null) return null;
            return int.TryParse(v.ToString(), out var x) ? x : null;
        }

        // Obtiene el ID del rol sin depender del nombre exacto de la propiedad (RolId o Id)
        private static int GetRolIdFlexible(object obj)
        {
            var t = obj?.GetType();
            if (t == null) return 0;

            var pRolId = t.GetProperty("RolId");
            if (pRolId != null)
            {
                var v = pRolId.GetValue(obj);
                if (v != null && int.TryParse(v.ToString(), out var id)) return id;
            }

            var pId = t.GetProperty("Id");
            if (pId != null)
            {
                var v = pId.GetValue(obj);
                if (v != null && int.TryParse(v.ToString(), out var id2)) return id2;
            }

            return 0;
        }

        private async Task CargarRolesAsync(string token, int? seleccionadoId = null, string? seleccionadoNombre = null)
        {
            try
            {
                var roles = await _roles.GetAllAsync(token) ?? new List<RolResponseDTO>();

                // Preselección por nombre si no tenemos Id
                if (!seleccionadoId.HasValue && !string.IsNullOrWhiteSpace(seleccionadoNombre))
                {
                    var encontrado = roles.FirstOrDefault(r =>
                        string.Equals(r.Nombre, seleccionadoNombre, StringComparison.OrdinalIgnoreCase));
                    if (encontrado != null) seleccionadoId = GetRolIdFlexible(encontrado);
                }

                // Construimos los items sin depender de nombres de propiedades
                var items = roles
                    .Select(r => new SelectListItem
                    {
                        Value = GetRolIdFlexible(r).ToString(),
                        Text = r.Nombre
                    })
                    .Where(i => i.Text?.Length > 0 && i.Value != "0")
                    .ToList();

                ViewBag.Roles = items;

                if (items.Count == 0)
                    TempData["Error"] = "No hay roles cargados o accesibles.";
            }
            catch (ApiHttpException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "No tienes permisos para cargar los roles (403).";
                ViewBag.Roles = new List<SelectListItem>();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar roles: {ex.Message}";
                ViewBag.Roles = new List<SelectListItem>();
            }
        }

        // ------------------------------
        // Index / Details
        // ------------------------------
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

        // ------------------------------
        // Create
        // ------------------------------
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            await CargarRolesAsync(token);
            return View(new UsuarioCreateDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(UsuarioCreateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                await CargarRolesAsync(token, dto.RolId);
                return View(dto);
            }

            try
            {
                var created = await _svc.CreateAsync(dto, token);
                if (created is null)
                {
                    ViewBag.Error = "No se pudo crear el usuario.";
                    await CargarRolesAsync(token, dto.RolId);
                    return View(dto);
                }
                TempData["Ok"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                await CargarRolesAsync(token, dto.RolId);
                return View(dto);
            }
        }

        // ------------------------------
        // Edit
        // ------------------------------
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            var u = await _svc.GetByIdAsync(id, token);
            if (u is null) return NotFound();

            // Intenta leer RolId y NombreRol/Rol desde el DTO devuelto por la API
            int? rolId = u.GetType().GetProperty("RolId")?.GetValue(u) is object rid && int.TryParse($"{rid}", out var ridInt) ? ridInt : (int?)null;
            string rolNombre =
                u.GetType().GetProperty("Rol")?.GetValue(u)?.ToString()
                ?? u.GetType().GetProperty("NombreRol")?.GetValue(u)?.ToString()
                ?? "(sin rol)";

            // Guardamos para la vista (solo mostrar)
            ViewBag.RolActual = rolNombre;
            ViewBag.Id = id;

            var vm = new UsuarioUpdateDTO
            {
                Nombre = u.GetType().GetProperty("Nombre")?.GetValue(u)?.ToString() ?? string.Empty,
                Email = u.GetType().GetProperty("Email")?.GetValue(u)?.ToString() ?? string.Empty,
                Direccion = u.GetType().GetProperty("Direccion")?.GetValue(u)?.ToString() ?? string.Empty,
                Telefono = u.GetType().GetProperty("Telefono")?.GetValue(u)?.ToString() ?? string.Empty,
                RolId = rolId ?? 0 // lo enviamos escondido para mantenerlo, pero NO editable
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, UsuarioUpdateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            // Rol congelado (como ya lo tenías)
            var current = await _svc.GetByIdAsync(id, token);
            if (current is null) return NotFound();
            dto.RolId = current.GetType().GetProperty("RolId")?.GetValue(current) is object rid
                        && int.TryParse($"{rid}", out var ridInt) ? ridInt : 0;

            if (!ModelState.IsValid)
            {
                ViewBag.RolActual = current.GetType().GetProperty("Rol")?.GetValue(current)?.ToString()
                                    ?? current.GetType().GetProperty("NombreRol")?.GetValue(current)?.ToString()
                                    ?? "(sin rol)";
                ViewBag.Id = id;
                return View(dto);
            }

            try
            {
                await _svc.UpdateAsync(id, dto, token); // <- si no lanza, fue OK
                TempData["Ok"] = "Usuario actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiHttpException ex)
            {
                ViewBag.Error = $"{(int)ex.StatusCode} {ex.Message}";
                ViewBag.RolActual = current.GetType().GetProperty("Rol")?.GetValue(current)?.ToString()
                                    ?? current.GetType().GetProperty("NombreRol")?.GetValue(current)?.ToString()
                                    ?? "(sin rol)";
                ViewBag.Id = id;
                return View(dto);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.RolActual = current.GetType().GetProperty("Rol")?.GetValue(current)?.ToString()
                                    ?? current.GetType().GetProperty("NombreRol")?.GetValue(current)?.ToString()
                                    ?? "(sin rol)";
                ViewBag.Id = id;
                return View(dto);
            }
        }

        // ------------------------------
        // Delete
        // ------------------------------
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
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
        [Authorize(Policy = "AdminOnly")]
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
