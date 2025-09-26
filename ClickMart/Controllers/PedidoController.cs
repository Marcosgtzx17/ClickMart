using ClickMart.DTOs.CodigoConfirmacionDTOs;
using ClickMart.DTOs.PedidoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClickMart.Utils;        
using System.Security.Claims; 

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente,Admin")]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidos;
        private readonly ICodigoConfirmacionService _codigos;
        private readonly IFacturaService _facturas;

        public PedidoController(IPedidoService pedidos, ICodigoConfirmacionService codigos, IFacturaService facturas)
        {
            _pedidos = pedidos;
            _codigos = codigos;
            _facturas = facturas;
        }

        // SOLO Admin: ver todos
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<PedidoResponseDTO>>> GetAll() =>
            Ok(await _pedidos.GetAllAsync());

        // Mis pedidos (Cliente) o Admin (todos de un usuario)
        [HttpGet("mios")]
        public async Task<ActionResult<List<PedidoResponseDTO>>> GetMine()
        {
            var uid = User.GetUserId();
            if (uid is null) return Unauthorized();
            return Ok(await _pedidos.GetByUsuarioAsync(uid.Value));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoResponseDTO>> GetById(int id)
        {
            var dto = await _pedidos.GetByIdAsync(id);
            if (dto is null) return NotFound();

            if (!User.IsAdmin() && User.GetUserId() != dto.UsuarioId) return Forbid();
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<PedidoResponseDTO>> Create([FromBody] PedidoCreateDTO dto)
        {
            // Cliente: ignora el UsuarioId que venga y fuerza el del token
            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null) return Unauthorized();
                dto.UsuarioId = uid.Value;
            }

            try
            {
                var created = await _pedidos.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.PedidoId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PedidoUpdateDTO dto)
        {
            var current = await _pedidos.GetByIdAsync(id);
            if (current is null) return NotFound();
            if (!User.IsAdmin() && User.GetUserId() != current.UsuarioId) return Forbid();

            var ok = await _pedidos.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var current = await _pedidos.GetByIdAsync(id);
            if (current is null) return NotFound();
            if (!User.IsAdmin() && User.GetUserId() != current.UsuarioId) return Forbid();

            var ok = await _pedidos.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/recalcular-total")]
        public async Task<IActionResult> RecalcularTotal(int id)
        {
            var current = await _pedidos.GetByIdAsync(id);
            if (current is null) return NotFound();
            if (!User.IsAdmin() && User.GetUserId() != current.UsuarioId) return Forbid();

            var total = await _pedidos.RecalcularTotalAsync(id);
            return Ok(new { pedidoId = id, total });
        }

        // POST /api/pedido/{id}/generar-codigo
        [HttpPost("{id:int}/generar-codigo")]
        public async Task<ActionResult<CodigoConfirmacionResponseDTO>> GenerarCodigoParaPedido(int id)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();
            if (!User.IsAdmin() && User.GetUserId() != pedido.UsuarioId) return Forbid();

            if (pedido.PagoEstado != EstadoPago.PENDIENTE || (pedido.Total ?? 0m) <= 0m)
                return BadRequest(new { message = "Pedido no listo para generar código." });

            var email = User.GetEmail();           
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "No se encontró el correo en el token." });

            var dto = await _codigos.GenerarAsync(email);
            return Ok(dto);
        }

        // POST /api/pedido/{id}/confirmar
        [HttpPost("{id:int}/confirmar")]
        public async Task<IActionResult> ConfirmarPago(int id, [FromBody] CodigoValidarDTO dto)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();
            if (!User.IsAdmin() && User.GetUserId() != pedido.UsuarioId) return Forbid();

            var email = User.GetEmail();          
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "No se encontró el correo en el token." });

            var valid = await _codigos.ValidarAsync(email, dto.Codigo);
            if (!valid) return BadRequest(new { message = "Código inválido o expirado." });

            var ok = await _pedidos.MarcarPagadoAsync(id);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("{id:int}/factura")]
        public async Task<IActionResult> DescargarFactura(int id)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();
            if (!User.IsAdmin() && User.GetUserId() != pedido.UsuarioId) return Forbid();

            var pdf = await _facturas.GenerarFacturaPdfAsync(id);
            return pdf is null ? NotFound() : File(pdf, "application/pdf", $"Factura_{id}.pdf");
        }
    }
}
