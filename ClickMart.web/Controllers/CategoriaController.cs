using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ClickMart.web.DTOs.CategoriaDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;

namespace ClickMart.web.Controllers
{
    [Authorize] // autenticado para ver/consultar
    public class CategoriaController : Controller
    {
        private readonly CategoriaService _svc;

        public CategoriaController(CategoriaService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var list = await _svc.GetAllAsync(token) ?? new List<CategoriaResponseDTO>();
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<CategoriaResponseDTO>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")] // mutaciones solo admin
        public IActionResult Create() => View(new CategoriaCreateDTO());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CategoriaCreateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (!ModelState.IsValid) return View(dto);

            try
            {
                var created = await _svc.CreateAsync(dto, token);
                if (created is null) { ViewBag.Error = "No se pudo crear la categoría."; return View(dto); }
                TempData["Ok"] = "Categoría creada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var cat = await _svc.GetByIdAsync(id, token);
            if (cat is null) return NotFound();

            var vm = new CategoriaUpdateDTO { Nombre = cat.Nombre };
            ViewBag.Id = id;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, CategoriaUpdateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (!ModelState.IsValid) { ViewBag.Id = id; return View(dto); }

            try
            {
                var ok = await _svc.UpdateAsync(id, dto, token);
                if (!ok) { ViewBag.Error = "No se pudo actualizar la categoría."; ViewBag.Id = id; return View(dto); }
                TempData["Ok"] = "Categoría actualizada.";
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id, CategoriaResponseDTO model)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            try
            {
                var ok = await _svc.DeleteAsync(id, token!);
                TempData[ok ? "Success" : "Error"] = ok
                    ? "Categoría eliminada."
                    : "No se pudo eliminar la categoría.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiHttpException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Re-render con alerta bonita (igual que Distribuidor)
                ModelState.AddModelError(string.Empty, "La categoría está en uso y no puede eliminarse.");

                // Intentar leer conteo de productos vinculados desde el body JSON del error (si existe)
                var body = ex.GetType().GetProperty("ResponseBody")?.GetValue(ex) as string; // reflection-safe
                int? productos = TryExtractProductos(body);

                ViewBag.BlockInfo = new
                {
                    Message = ex.Message, // texto del backend (o pon un copy propio)
                    Productos = productos
                };

                return View(model);
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // === Helpers ===
        private static int? TryExtractProductos(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                // esperamos un shape como: { message, detalles: { productos: N } }
                if (doc.RootElement.TryGetProperty("detalles", out var det) &&
                    det.TryGetProperty("productos", out var prod) &&
                    prod.TryGetInt32(out var n))
                {
                    return n;
                }
            }
            catch { /* no-op: UX sigue mostrando el alert sin conteo */ }
            return null;
        }
    }
}