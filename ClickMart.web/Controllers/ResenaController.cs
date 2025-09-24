using ClickMart.web.DTOs.ResenaDTOs;
using ClickMart.web.DTOs.ProductoDTOs;
using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClickMart.web.Controllers
{
    [Authorize]
    public class ResenaController : Controller
    {
        private readonly ResenaService _svc;
        private readonly UsuarioService _usuarios;
        private readonly ProductoService _productos;

        public ResenaController(ResenaService svc, UsuarioService usuarios, ProductoService productos)
        {
            _svc = svc;
            _usuarios = usuarios;
            _productos = productos;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var data = await _svc.GetAllAsync(token) ?? new List<ResenaResponseDTO>();

                // Mapear nombres para mostrar en tabla
                var us = await _usuarios.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
                var prods = await _productos.GetAllAsync(token) ?? new List<ProductoResponseDTO>();
                var mapUsers = us.ToDictionary(x => x.UsuarioId, x => x.Email ?? x.Nombre ?? $"Usuario {x.UsuarioId}");
                var mapProds = prods.ToDictionary(x => x.ProductoId, x => x.Nombre);

                ViewBag.UserNames = mapUsers;
                ViewBag.ProductNames = mapProds;

                return View(data);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<ResenaResponseDTO>());
            }
        }

        private async Task PopulateCombos(ResenaFormVM vm, string? token)
        {
            var us = await _usuarios.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
            vm.Usuarios = us.Select(u => new SelectListItem
            {
                Value = u.UsuarioId.ToString(),
                Text = string.IsNullOrWhiteSpace(u.Email) ? (u.Nombre ?? $"Usuario {u.UsuarioId}") : u.Email
            }).ToList();

            var prods = await _productos.GetAllAsync(token) ?? new List<ProductoResponseDTO>();
            vm.Productos = prods.Select(p => new SelectListItem { Value = p.ProductoId.ToString(), Text = p.Nombre }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var token = ClaimsHelper.GetToken(User);
            var vm = new ResenaFormVM { Calificacion = 5, FechaResena = DateTime.UtcNow };
            await PopulateCombos(vm, token);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResenaFormVM vm)
        {
            var token = ClaimsHelper.GetToken(User);
            if (!ModelState.IsValid)
            {
                await PopulateCombos(vm, token);
                return View(vm);
            }

            try
            {
                var dto = new ResenaCreateDTO
                {
                    UsuarioId = vm.UsuarioId,
                    ProductoId = vm.ProductoId,
                    Calificacion = vm.Calificacion,
                    Comentario = vm.Comentario,
                    FechaResena = vm.FechaResena
                };
                var created = await _svc.CreateAsync(dto, token!);
                TempData["Ok"] = "Reseña creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await PopulateCombos(vm, token);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var data = await _svc.GetByIdAsync(id, token);
            if (data is null) return NotFound();

            var vm = new ResenaFormVM
            {
                ResenaId = data.ResenaId,
                UsuarioId = data.UsuarioId,
                ProductoId = data.ProductoId,
                Calificacion = data.Calificacion,
                Comentario = data.Comentario,
                FechaResena = data.FechaResena
            };
            await PopulateCombos(vm, token);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResenaFormVM vm)
        {
            var token = ClaimsHelper.GetToken(User);
            if (!ModelState.IsValid)
            {
                await PopulateCombos(vm, token);
                return View(vm);
            }
            try
            {
                var dto = new ResenaUpdateDTO
                {
                    Calificacion = vm.Calificacion,
                    Comentario = vm.Comentario,
                    FechaResena = vm.FechaResena
                };
                var ok = await _svc.UpdateAsync(id, dto, token!);
                if (!ok) TempData["Error"] = "No se pudo actualizar la reseña.";
                else TempData["Ok"] = "Reseña actualizada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await PopulateCombos(vm, token);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var ok = await _svc.DeleteAsync(id, token!);
                if (!ok) TempData["Error"] = "No se pudo eliminar la reseña.";
                else TempData["Ok"] = "Reseña eliminada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}