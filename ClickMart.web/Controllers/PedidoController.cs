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
            var roleTypes = new[] { ClaimTypes.Role, "role", "roles", "rol" };
            var roles = u.Claims
                .Where(c => roleTypes.Contains(c.Type))
                .SelectMany(c => (c.Value ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(v => v.Trim().ToLowerInvariant());

            // acepta "admin", "administrador", "administrator"
            return roles.Any(r => r == "admin" || r == "administrador" || r == "administrator");
        }

        private int? GetMyUserId()
        {
            var raw = User.FindFirst("uid")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.Identity?.Name;

            return int.TryParse(raw, out var id) ? id : null;
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

                return View(data);
            }
            catch (ApiHttpException ex)
            {
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

            PedidoResponseDTO p;
            try
            {
                p = await _svc.GetByIdAsync(id, token);
                if (p is null) return NotFound();
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = $"No se pudo obtener el pedido (API {(int)ex.StatusCode}): {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            // ¿es dueño?
            var myId = GetMyUserId() ?? 0;
            var esDueno = myId == p.UsuarioId;

            // *** Regla pedida: solo gestiona si es dueño y el pago está PENDIENTE ***
            var puedeGestionar = esDueno && p.PagoEstado == EstadoPagoDTO.PENDIENTE;
            ViewBag.PuedeGestionar = puedeGestionar; // admin NO gestiona

            // Obtener detalles con manejo de 403
            List<DetallePedidoResponseDTO> detalles;
            try
            {
                detalles = await _detSvc.GetByPedidoAsync(id, token) ?? new List<DetallePedidoResponseDTO>();
            }
            catch (ApiHttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                // Si eres admin y la API vieja no permite, muestra vacío en vez de romper.
                if (User.IsInRole("Admin") || User.IsInRole("Administrador") || User.IsInRole("administrator"))
                {
                    detalles = new List<DetallePedidoResponseDTO>();
                    TempData["Warn"] = "No se pudieron cargar los detalles por permisos del endpoint. (Actualiza la API para permitir Admin)";
                }
                else
                {
                    TempData["Error"] = "No tienes permisos para ver ese pedido.";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Detalles = detalles;

            // estos no son críticos; si fallan, no bloquean la vista
            try
            {
                var productos = await _prodSvc.GetAllAsync(token) ?? new List<ProductoLiteDTO>();
                ViewBag.Productos = productos;
            }
            catch { ViewBag.Productos = new List<ProductoLiteDTO>(); }

            ViewBag.NewDetalle = new DetallePedidoCreateDTO { IdPedido = id, Cantidad = 1 };

            return View(p);
        }

        // Helper: ¿el usuario autenticado puede gestionar (dueño + PENDIENTE)?
        private async Task<bool> PuedeGestionarPedidoAsync(int pedidoId, string token)
        {
            var p = await _svc.GetByIdAsync(pedidoId, token);
            if (p is null) return false;

            var myId = GetMyUserId();
            return myId.HasValue
                && myId.Value == p.UsuarioId
                && p.PagoEstado == EstadoPagoDTO.PENDIENTE;
        }

        // (Se deja el antiguo por compatibilidad; ya no lo usamos en mutaciones)
        private async Task<bool> EsDuenoDelPedidoAsync(int pedidoId, string token)
        {
            var p = await _svc.GetByIdAsync(pedidoId, token);
            if (p is null) return false;

            var myId = GetMyUserId();
            return myId.HasValue && myId.Value == p.UsuarioId;
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Ahora requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes editar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

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

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes editar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

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

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes eliminar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

            var p = await _svc.GetByIdAsync(id, token!);
            return p is null ? NotFound() : View(p);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes eliminar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

            var ok = await _svc.DeleteAsync(id, token!);
            TempData[ok ? "Success" : "Error"] = ok ? "Pedido eliminado." : "No se pudo eliminar.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RecalcularTotal(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes modificar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var result = await _svc.RecalcularTotalAsync(id, token!);
                TempData[result is null ? "Error" : "Success"] =
                    result is null ? "No se pudo recalcualar el total." : $"Total recalculado: {result.Total:C}";
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
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes generar código para este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var dto = await _svc.GenerarCodigoAsync(id, token!);
                TempData["Success"] = dto is null
                    ? "No se pudo generar el código."
                    : $"Código generado y enviado: {dto.Codigo}";
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Confirmar(int id, CodigoValidarDTO dto)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Requiere dueño + PENDIENTE (confirmar sólo tiene sentido si está pendiente)
            if (!await PuedeGestionarPedidoAsync(id, token!))
            {
                TempData["Error"] = "No puedes confirmar pagos de este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(dto.Codigo))
            {
                TempData["Error"] = "Debe ingresar el código.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var ok = await _svc.ConfirmarPagoAsync(id, dto.Codigo, token!);
                TempData[ok ? "Success" : "Error"] = ok ? "Pago confirmado." : "No se pudo confirmar.";
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> AddDetalle(DetallePedidoCreateDTO dto)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(dto.IdPedido, token!))
            {
                TempData["Error"] = "No puedes modificar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id = dto.IdPedido });
            }

            if (dto.IdPedido <= 0) { TempData["Error"] = "Pedido inválido."; return RedirectToAction(nameof(Index)); }
            if (dto.Cantidad <= 0) { TempData["Error"] = "Cantidad debe ser mayor a 0."; return RedirectToAction(nameof(Details), new { id = dto.IdPedido }); }

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

            try { await _svc.RecalcularTotalAsync(dto.IdPedido, token!); }
            catch { TempData["Warn"] = "Detalle agregado, pero no se pudo recalcular el total ahora."; }

            return RedirectToAction(nameof(Details), new { id = dto.IdPedido });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDetalle(DetallePedidoUpdateDTO dto, int pedidoId)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(pedidoId, token!))
            {
                TempData["Error"] = "No puedes modificar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id = pedidoId });
            }

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

            // Requiere dueño + PENDIENTE
            if (!await PuedeGestionarPedidoAsync(pedidoId, token!))
            {
                TempData["Error"] = "No puedes modificar este pedido (no eres dueño o no está PENDIENTE).";
                return RedirectToAction(nameof(Details), new { id = pedidoId });
            }

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

        // =================== NUEVO: Ver Factura (PDF) ===================
        [HttpGet]
        public async Task<IActionResult> Factura(int id)
        {
            if (TokenInvalido(out var token)) return RedirectToAction("Login", "Auth");

            try
            {
                var pdfBytes = await _svc.GetFacturaPdfAsync(id, token!);
                if (pdfBytes is null || pdfBytes.Length == 0)
                {
                    TempData["Error"] = "No se encontró la factura.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                Response.Headers["Content-Disposition"] = $"inline; filename=Factura_{id}.pdf";
                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                return File(pdfBytes, "application/pdf");
            }
            catch (ApiHttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "No tienes permisos para ver ese PDF.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ApiHttpException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
