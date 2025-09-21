using ClickMart.web.DTOs.ProductoDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // IFormFile

namespace ClickMart.web.Controllers
{
    // ✅ Solo requiere estar autenticado para ver listado/detalle
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly ProductoService _svc;

        public ProductoController(ProductoService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var list = await _svc.GetAllAsync(token);
                return View(list ?? new List<ProductoResponseDTO>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<ProductoResponseDTO>());
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

        // 🔒 Solo Admin: mostrar formulario de alta
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create() => View(new ProductoCreateDTO());

        // 🔒 Solo Admin: crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(ProductoCreateDTO dto, IFormFile? ImagenArchivo)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return View(dto);

            try
            {
                var created = await _svc.CreateAsync(dto, token);
                if (created is null)
                {
                    ViewBag.Error = "No se pudo crear el producto.";
                    return View(dto);
                }

                if (ImagenArchivo != null && ImagenArchivo.Length > 0)
                    await _svc.UploadImageAsync(created.ProductoId, ImagenArchivo, token);

                TempData["Ok"] = "Producto creado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }

        // 🔒 Solo Admin: cargar pantalla de edición
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var p = await _svc.GetByIdAsync(id, token);
            if (p is null) return NotFound();

            var vm = new ProductoUpdateDTO
            {
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Talla = p.Talla,
                Precio = p.Precio,
                Marca = p.Marca,
                Stock = p.Stock,
                CategoriaId = p.CategoriaId,
                DistribuidorId = p.DistribuidorId
            };
            ViewBag.Id = id;
            return View(vm);
        }

        // 🔒 Solo Admin: actualizar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, ProductoUpdateDTO dto, IFormFile? ImagenArchivo)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) { ViewBag.Id = id; return View(dto); }

            try
            {
                var ok = await _svc.UpdateAsync(id, dto, token);
                if (!ok)
                {
                    ViewBag.Error = "No se pudo actualizar el producto.";
                    ViewBag.Id = id;
                    return View(dto);
                }

                if (ImagenArchivo != null && ImagenArchivo.Length > 0)
                    await _svc.UploadImageAsync(id, ImagenArchivo, token);

                TempData["Ok"] = "Producto actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Id = id;
                return View(dto);
            }
        }

        // 🔒 Solo Admin: confirmar eliminación
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        // 🔒 Solo Admin: eliminar
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var ok = await _svc.DeleteAsync(id, token);
                if (!ok) TempData["Error"] = "No se pudo eliminar el producto.";
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
