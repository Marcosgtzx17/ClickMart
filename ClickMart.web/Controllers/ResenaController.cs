// ClickMart.web/Controllers/ResenaController.cs
using ClickMart.web.DTOs.ResenaDTOs;
using ClickMart.web.DTOs.ProductoDTOs;
using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

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

        // Helpers
        private static string DisplayUser(UsuarioListadoDTO u) =>
            string.IsNullOrWhiteSpace(u.Email) ? (u.Nombre ?? $"Usuario {u.UsuarioId}") : u.Email;

        private string? GetEmail() =>
            User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        private async Task PopulateCombos(ResenaFormVM vm, string? token)
        {
            var us = await _usuarios.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
            vm.Usuarios = us.Select(u => new SelectListItem
            {
                Value = u.UsuarioId.ToString(),
                Text = DisplayUser(u)
            }).ToList();

            var prods = await _productos.GetAllAsync(token) ?? new List<ProductoResponseDTO>();
            vm.Productos = prods.Select(p => new SelectListItem
            {
                Value = p.ProductoId.ToString(),
                Text = p.Nombre
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            try
            {
                var data = await _svc.GetAllAsync(token) ?? new List<ResenaResponseDTO>();

                // Para mostrar nombres en la tabla
                var us = await _usuarios.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
                var prods = await _productos.GetAllAsync(token) ?? new List<ProductoResponseDTO>();
                var mapUsers = us.ToDictionary(x => x.UsuarioId, DisplayUser);
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

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var token = ClaimsHelper.GetToken(User);

            var vm = new ResenaFormVM
            {
                Calificacion = 5,
                FechaResena = DateTime.UtcNow
            };

            // --- Autocompletar email + UsuarioId del usuario logueado ---
            // 1) Obtener email desde Claims
            vm.UsuarioEmail = GetEmail();

            // 2) Resolver UsuarioId buscando por email (para pasar la validación del VM)
            if (!string.IsNullOrWhiteSpace(vm.UsuarioEmail))
            {
                var users = await _usuarios.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
                var match = users.FirstOrDefault(u =>
                    string.Equals(u.Email, vm.UsuarioEmail, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    vm.UsuarioId = match.UsuarioId; // pre-llenar
                }
            }
            // Nota: si no encontrara match, el backend igual forzará el UsuarioId del token.

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
                    UsuarioId = vm.UsuarioId,   // El backend lo forzará al usuario autenticado si no es Admin
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

            // Mostrar autor en solo lectura (nombre y email si lo tenemos)
            var usuarios = await _usuarios.GetAllAsync(token) ?? new List<UsuarioListadoDTO>();
            var byId = usuarios.FirstOrDefault(u => u.UsuarioId == data.UsuarioId);
            vm.UsuarioNombre = byId != null ? DisplayUser(byId) : data.UsuarioId.ToString();
            vm.UsuarioEmail = byId?.Email; // opcional en Edit (solo display)

            await PopulateCombos(vm, token); // combo Usuario no se usa en Edit (queda bloqueado en la vista)
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