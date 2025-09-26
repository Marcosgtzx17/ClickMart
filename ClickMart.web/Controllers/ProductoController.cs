using ClickMart.web.DTOs.ProductoDTOs;
using ClickMart.web.DTOs.ResenaDTOs; // <-- NUEVO (para tipos si los usas)
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClickMart.web.Controllers
{
    [Authorize]
    [Route("[controller]")] // => base /Producto
    public class ProductoController : Controller
    {
        private readonly ProductoService _svc;
        private readonly CatalogoService _catalogo;
        private readonly ResenaService _resenas; // <-- NUEVO

        public ProductoController(ProductoService svc, CatalogoService catalogo, ResenaService resenas) // <-- NUEVO
        {
            _svc = svc;
            _catalogo = catalogo;
            _resenas = resenas; // <-- NUEVO
        }

        // GET /Producto
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var list = await _svc.GetAllAsync(token) ?? new List<ProductoResponseDTO>();

                // === NUEVO: calcular calificación promedio por producto ===
                var ratings = new Dictionary<int, double>();
                foreach (var p in list)
                {
                    var resenas = await _resenas.GetByProductoAsync(p.ProductoId, token) ?? new List<ResenaResponseDTO>();
                    ratings[p.ProductoId] = resenas.Count > 0 ? resenas.Average(r => r.Calificacion) : 0.0;
                }
                ViewBag.Ratings = ratings; // idProducto -> promedio

                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Ratings = new Dictionary<int, double>(); // para evitar nulls en la vista
                return View(new List<ProductoResponseDTO>());
            }
        }

        // GET /Producto/Details/5
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        // GET /Producto/Create
        [HttpGet("Create")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            var token = ClaimsHelper.GetToken(User);
            await CargarCombosAsync(token);
            return View(new ProductoCreateDTO());
        }

        // POST /Producto/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(ProductoCreateDTO dto, IFormFile? ImagenArchivo)
        {
            var token = ClaimsHelper.GetToken(User);
            if (!ModelState.IsValid) { await CargarCombosAsync(token); return View(dto); }

            try
            {
                var created = await _svc.CreateAsync(dto, token);
                if (created is null)
                {
                    ViewBag.Error = "No se pudo crear el producto.";
                    await CargarCombosAsync(token);
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
                await CargarCombosAsync(token);
                return View(dto);
            }
        }

        // GET /Producto/Edit/5
        [HttpGet("Edit/{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var p = await _svc.GetByIdAsync(id, token);
            if (p is null) return NotFound();

            await CargarCombosAsync(token);

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

        // POST /Producto/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, ProductoUpdateDTO dto, IFormFile? ImagenArchivo)
        {
            var token = ClaimsHelper.GetToken(User);
            if (!ModelState.IsValid) { ViewBag.Id = id; await CargarCombosAsync(token); return View(dto); }

            try
            {
                var ok = await _svc.UpdateAsync(id, dto, token);
                if (!ok)
                {
                    ViewBag.Error = "No se pudo actualizar el producto.";
                    ViewBag.Id = id; await CargarCombosAsync(token);
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
                ViewBag.Id = id; await CargarCombosAsync(token);
                return View(dto);
            }
        }

        // GET /Producto/Delete/5
        [HttpGet("Delete/{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var item = await _svc.GetByIdAsync(id, token);
            if (item is null) return NotFound();
            return View(item);
        }

        // POST /Producto/Delete/5
        [HttpPost("Delete/{id:int}")]
        [ActionName("Delete")]
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

        // ---------- Helpers ----------
        private async Task CargarCombosAsync(string? token)
        {
            var categorias = await _catalogo.GetCategoriasAsync(token)
                             ?? new List<ClickMart.web.DTOs.CatalogoDTOs.CategoriaDTO>();
            var distribuidores = await _catalogo.GetDistribuidoresAsync(token)
                                ?? new List<ClickMart.web.DTOs.CatalogoDTOs.DistribuidorDTO>();

            ViewBag.Categorias = new SelectList(categorias, "CategoriaId", "Nombre");
            ViewBag.Distribuidores = new SelectList(distribuidores, "DistribuidorId", "Nombre");
        }
    }
}
