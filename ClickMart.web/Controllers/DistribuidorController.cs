using ClickMart.web.DTOs.DistribuidorDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace ClickMart.web.Controllers
{
   
    [Authorize(Policy = "AdminOnly")]
    public class DistribuidorController : Controller
    {
        private readonly DistribuidorService _svc;

        public DistribuidorController(DistribuidorService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            try
            {
                var data = await _svc.GetAllAsync(token) ?? new List<DistribuidorResponseDTO>();
                return View(data);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"No se pudieron cargar los distribuidores: {ex.Message}";
                return View(new List<DistribuidorResponseDTO>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var vm = await _svc.GetByIdAsync(id, token);
            return vm is null ? NotFound() : View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new DistribuidorCreateDTO());

        // POST: /Distribuidor/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistribuidorCreateDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var token = ClaimsHelper.GetToken(User);
            var created = await _svc.CreateAsync(dto, token);
            if (created is null)
            {
                TempData["Error"] = "No se pudo crear el distribuidor.";
                return View(dto);
            }

            TempData["Success"] = "Distribuidor creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var r = await _svc.GetByIdAsync(id, token);
            if (r is null) return NotFound();

            var vm = new DistribuidorUpdateDTO
            {
                DistribuidorId = r.DistribuidorId,
                Nombre = r.Nombre,
                Direccion = r.Direccion,
                Telefono = r.Telefono,
                Gmail = r.Gmail,
                Descripcion = r.Descripcion,
                FechaRegistro = r.FechaRegistro
            };
            return View(vm);
        }

        // POST: /Distribuidor/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DistribuidorUpdateDTO dto)
        {
            if (id != dto.DistribuidorId) return BadRequest();
            if (!ModelState.IsValid) return View(dto);

            var token = ClaimsHelper.GetToken(User);
            var ok = await _svc.UpdateAsync(dto, token);
            if (!ok)
            {
                TempData["Error"] = "No se pudo actualizar el distribuidor.";
                return View(dto);
            }

            TempData["Success"] = "Distribuidor actualizado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var vm = await _svc.GetByIdAsync(id, token);
            return vm is null ? NotFound() : View(vm);
        }

        // POST: /Distribuidor/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection _)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            try
            {
                await _svc.DeleteAsync(id, token!);
                TempData["Success"] = "Distribuidor eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiHttpException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Intenta parsear el JSON devuelto por la API para enriquecer la UI
                string friendlyMessage = "No se puede eliminar el distribuidor porque está en uso.";
                int? productos = null;

                try
                {
                    using var doc = JsonDocument.Parse(ex.ResponseBody ?? "{}");
                    var root = doc.RootElement;

                    if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        friendlyMessage = msg.GetString()!;

                    if (root.TryGetProperty("detalles", out var det) && det.ValueKind == JsonValueKind.Object)
                    {
                        if (det.TryGetProperty("productos", out var p) && p.ValueKind == JsonValueKind.Number)
                            productos = p.GetInt32();
                    }
                }
                catch { /* swallow parse errors */ }

                // Rehidrata el modelo para la vista Delete
                var model = await _svc.GetByIdAsync(id, token!) ?? new DTOs.DistribuidorDTOs.DistribuidorResponseDTO();

                ViewBag.BlockInfo = new
                {
                    Message = friendlyMessage,
                    Productos = productos
                };

                // Agrega un error al ModelState para que la vista lo muestre
                ModelState.AddModelError(string.Empty, friendlyMessage);
                Response.StatusCode = (int)HttpStatusCode.Conflict; // útil si hay tests/UI

                return View(model);
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = $"No se pudo eliminar: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // Index placeholder (asume que existe)
        
    }
}

