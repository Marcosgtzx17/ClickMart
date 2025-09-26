using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using ClickMart.web.DTOs.PedidoDTOs;
using ClickMart.web.DTOs.DetallePedidoDTOs;
using ClickMart.web.DTOs.ProductoDTOs;
using ClickMart.web.DTOs.UsuarioDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Models;
using ClickMart.web.Services;

namespace ClickMart.web.Controllers
{
    [Authorize]
    public class PedidoController : Controller
    {
        private readonly PedidoService _svc;
        private readonly DetallePedidoService _detSvc;
        private readonly ProductoCatalogService _prodSvc;
        private readonly UsuarioApiService _usuarios;

        public PedidoController(
            PedidoService svc,
            DetallePedidoService detSvc,
            ProductoCatalogService prodSvc,
            UsuarioApiService usuarios)
        {
            _svc = svc;
            _detSvc = detSvc;
            _prodSvc = prodSvc;
            _usuarios = usuarios;
        }

        // ----------------- Utilidad local -----------------
        private bool TokenInvalido(out string? token)
        {
            token = ClaimsHelper.GetToken(User);
            return string.IsNullOrWhiteSpace(token) || ClaimsHelper.IsJwtExpired(token);
        }

        private static bool EsAdmin(ClaimsPrincipal u)
        {
            var roles = u.FindAll(ClaimTypes.Role)
                         .Select(c => c.Value?.Trim().ToLowerInvariant())
                         .Where(v => !string.IsNullOrWhiteSpace(v));

            // acepta "admin", "administrador", "administrator"
            return roles.Any(r => r == "admin" || r == "administrador" || r == "administrator");
        }



        // ========== LISTADO ==========
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            try
            {
                var esAdmin = EsAdmin(User);
                ViewBag.EsAdmin = esAdmin;

                var data = esAdmin
                    ? (await _svc.GetAllAsync(token) ?? new List<PedidoResponseDTO>())
                    : (await _svc.GetMineAsync(token) ?? new List<PedidoResponseDTO>());

                //TempData["Warn"] = $"Modo {(esAdmin ? "ADMIN (GetAllAsync)" : "CLIENTE (GetMineAsync)")}. Roles: {string.Join(",", User.FindAll(ClaimTypes.Role).Select(r => r.Value))}";

                return View(data);
            }
            catch (ApiHttpException ex)
            {
                // Si tu token no tiene permiso para GetAll, verás 401/403 aquí.
                TempData["Error"] = $"No se pudieron cargar los pedidos: {ex.Message}";
                return View(new List<PedidoResponseDTO>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"No se pudieron cargar los pedidos: {ex.Message}";
                return View(new List<PedidoResponseDTO>());
            }
        }



        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

            try
            {
                var p = await _svc.GetByIdAsync(id, token);
                if (p is null) return NotFound();

                var detalles = await _detSvc.GetByPedidoAsync(id, token) ?? new List<DetallePedidoResponseDTO>();
                ViewBag.Detalles = detalles;
                ViewBag.NewDetalle = new DetallePedidoCreateDTO { IdPedido = id, Cantidad = 1 };
                ViewBag.Productos = await _prodSvc.GetAllAsync(token) ?? new List<ProductoLiteDTO>();

                return View(p);
            }
            catch (ApiHttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "No tienes permisos para ver ese pedido.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }


        // ========== CREATE ==========
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            var esAdmin = EsAdmin(User);
            ViewBag.EsAdmin = esAdmin;

            var vm = new PedidoCreateVM
            {
                Pedido = new PedidoCreateDTO
                {
                    Fecha = DateTime.Today,
                    MetodoPago = MetodoPagoDTO.EFECTIVO,
                    PagoEstado = EstadoPagoDTO.PENDIENTE
                },
                Usuarios = new List<SelectListItem>()
            };

            if (esAdmin)
            {
                var lista = await _usuarios.GetAllAsync(token!) ?? new List<UsuarioListadoDTO>();
                vm.Usuarios = lista.Select(u => new SelectListItem
                {
                    Value = u.UsuarioId.ToString(),
                    Text = $"{u.Nombre} ({u.Email})"
                }).ToList();
            }

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "Pedido")] PedidoCreateDTO dto)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            var esAdmin = EsAdmin(User);
            ViewBag.EsAdmin = esAdmin;

            dto.PagoEstado = EstadoPagoDTO.PENDIENTE;
            if (!esAdmin) dto.UsuarioId = 0;

            if (dto.MetodoPago == MetodoPagoDTO.TARJETA && string.IsNullOrWhiteSpace(dto.NumeroTarjeta))
            {
                ModelState.AddModelError(nameof(dto.NumeroTarjeta), "Debes ingresar el número de tarjeta.");
            }

            if (!ModelState.IsValid)
            {
                var vmError = new PedidoCreateVM { Pedido = dto, Usuarios = new List<SelectListItem>() };
                if (esAdmin)
                {
                    var lista = await _usuarios.GetAllAsync(token!) ?? new List<UsuarioListadoDTO>();
                    vmError.Usuarios = lista.Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = $"{u.Nombre} ({u.Email})"
                    }).ToList();
                }
                return View(vmError);
            }

            try
            {
                var creado = await _svc.CreateAsync(dto, token!);
                if (creado is null)
                {
                    TempData["Error"] = "No se pudo crear el pedido.";
                    var vmError = new PedidoCreateVM { Pedido = dto, Usuarios = new List<SelectListItem>() };
                    if (esAdmin)
                    {
                        var lista = await _usuarios.GetAllAsync(token!) ?? new List<UsuarioListadoDTO>();
                        vmError.Usuarios = lista.Select(u => new SelectListItem
                        {
                            Value = u.UsuarioId.ToString(),
                            Text = $"{u.Nombre} ({u.Email})"
                        }).ToList();
                    }
                    return View(vmError);
                }

                TempData["Success"] = "Pedido creado.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiHttpException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(nameof(dto.NumeroTarjeta), ex.Message);

                var vmError = new PedidoCreateVM { Pedido = dto, Usuarios = new List<SelectListItem>() };
                if (esAdmin)
                {
                    var lista = await _usuarios.GetAllAsync(token!) ?? new List<UsuarioListadoDTO>();
                    vmError.Usuarios = lista.Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = $"{u.Nombre} ({u.Email})"
                    }).ToList();
                }
                return View(vmError);
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
                var vmError = new PedidoCreateVM { Pedido = dto, Usuarios = new List<SelectListItem>() };
                if (esAdmin)
                {
                    var lista = await _usuarios.GetAllAsync(token!) ?? new List<UsuarioListadoDTO>();
                    vmError.Usuarios = lista.Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = $"{u.Nombre} ({u.Email})"
                    }).ToList();
                }
                return View(vmError);
            }
        }

        // ========== EDIT ==========
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            var p = await _svc.GetByIdAsync(id, token!);
            if (p is null) return NotFound();

            var vm = new PedidoUpdateDTO
            {
                PedidoId = p.PedidoId,
                Fecha = p.Fecha,
                MetodoPago = p.MetodoPago,
                PagoEstado = p.PagoEstado
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PedidoUpdateDTO dto)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            if (id != dto.PedidoId) return BadRequest();
            if (!ModelState.IsValid) return View(dto);

            var ok = await _svc.UpdateAsync(dto, token!);
            TempData[ok ? "Success" : "Error"] = ok ? "Pedido actualizado." : "No se pudo actualizar.";
            return ok ? RedirectToAction(nameof(Details), new { id = dto.PedidoId }) : View(dto);
        }

        // ========== DELETE ==========
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            var p = await _svc.GetByIdAsync(id, token!);
            return p is null ? NotFound() : View(p);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            var ok = await _svc.DeleteAsync(id, token!);
            TempData[ok ? "Success" : "Error"] = ok ? "Pedido eliminado." : "No se pudo eliminar.";
            return RedirectToAction(nameof(Index));
        }

        // ========== ACCIONES AUXILIARES ==========
        [HttpPost]
        public async Task<IActionResult> RecalcularTotal(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            try
            {
                var result = await _svc.RecalcularTotalAsync(id, token!);
                if (result is null)
                    TempData["Error"] = "No se pudo recalcualar el total.";
                else
                    TempData["Success"] = $"Total recalculado: {result.Total:C}";
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }
        [HttpPost]
        public async Task<IActionResult> GenerarCodigo(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth"); // sesión expirada

            // <- solo diagnóstico (no obligatorio)
            var email = ClickMart.Utils.ApiClaimsHelper.GetEmail(User);
            if (string.IsNullOrWhiteSpace(email))
                TempData["Warn"] = "No se encontró un email en tus claims (GetEmail).";

            try
            {
                var dto = await _svc.GenerarCodigoAsync(id, token);
                TempData["Success"] = dto is null
                    ? "No se pudo generar el código."
                    : $"Código generado y enviado: {dto.Codigo}";
            }
            catch (ApiHttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth"); // idéntico a la vez anterior

                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }



        [HttpPost]
        public async Task<IActionResult> Confirmar(int id, CodigoValidarDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                TempData["Error"] = "Debe ingresar el código.";
                return RedirectToAction(nameof(Details), new { id });
            }
            try
            {
                var ok = await _svc.ConfirmarPagoAsync(id, dto.Codigo, token);
                TempData[ok ? "Success" : "Error"] = ok ? "Pago confirmado." : "No se pudo confirmar.";
            }
            catch (ApiHttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth"); // mismo manejo

                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }


        [HttpPost]
        public async Task<IActionResult> AddDetalle(DetallePedidoCreateDTO dto)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            if (dto.IdPedido <= 0)
            {
                TempData["Error"] = "Pedido inválido.";
                return RedirectToAction(nameof(Index));
            }

            if (dto.Cantidad <= 0)
            {
                TempData["Error"] = "Cantidad debe ser mayor a 0.";
                return RedirectToAction(nameof(Details), new { id = dto.IdPedido });
            }

            try
            {
                var created = await _detSvc.CreateAsync(dto, token!);
                TempData[created is not null ? "Success" : "Error"] =
                    created is not null ? "Detalle agregado." : "No se pudo agregar el detalle.";
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = dto.IdPedido });
            }

            try
            {
                var result = await _svc.RecalcularTotalAsync(dto.IdPedido, token!);
                if (result is null)
                    TempData["Warn"] = "Detalle agregado, pero no se pudo recalcular el total ahora.";
            }
            catch (ApiHttpException)
            {
                TempData["Warn"] = "Detalle agregado, pero no se pudo recalcular el total ahora.";
            }

            return RedirectToAction(nameof(Details), new { id = dto.IdPedido });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDetalle(DetallePedidoUpdateDTO dto, int pedidoId)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            if (dto.DetalleId <= 0 || dto.Cantidad <= 0)
            {
                TempData["Error"] = "Datos inválidos.";
                return RedirectToAction(nameof(Details), new { id = pedidoId });
            }

            try
            {
                var ok = await _detSvc.UpdateAsync(dto, token!);
                TempData[ok ? "Success" : "Error"] = ok ? "Detalle actualizado." : "No se pudo actualizar.";
                await _svc.RecalcularTotalAsync(pedidoId, token!);
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDetalle(int id, int pedidoId)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            try
            {
                var ok = await _detSvc.DeleteAsync(id, token!);
                TempData[ok ? "Success" : "Error"] = ok ? "Detalle eliminado." : "No se pudo eliminar.";
                await _svc.RecalcularTotalAsync(pedidoId, token!);
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id = pedidoId });
        }
    }
}
