using ClickMart.DTOs.CodigoConfirmacionDTOs;
using ClickMart.DTOs.PedidoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidos;
        private readonly ICodigoConfirmacionService _codigos;

        public PedidoController(IPedidoService pedidos, ICodigoConfirmacionService codigos)
        {
            _pedidos = pedidos;
            _codigos = codigos;
        }

        // GET /api/pedido
        [HttpGet]
        public async Task<ActionResult<List<PedidoResponseDTO>>> GetAll() =>
            Ok(await _pedidos.GetAllAsync());

        // GET /api/pedido/123
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoResponseDTO>> GetById(int id)
        {
            var dto = await _pedidos.GetByIdAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // POST /api/pedido
        [HttpPost]
        public async Task<ActionResult<PedidoResponseDTO>> Create([FromBody] PedidoCreateDTO dto)
        {
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

        // PUT /api/pedido/123
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PedidoUpdateDTO dto)
        {
            var ok = await _pedidos.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/pedido/123
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _pedidos.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // POST /api/pedido/123/recalcular-total
        [HttpPost("{id:int}/recalcular-total")]
        public async Task<IActionResult> RecalcularTotal(int id)
        {
            try
            {
                var total = await _pedidos.RecalcularTotalAsync(id); // decimal
                return Ok(new { pedidoId = id, total });
            }
            catch (InvalidOperationException)
            {
                // Por si el servicio lanza "Pedido no encontrado."
                return NotFound();
            }
        }


        // POST /api/pedido/123/generar-codigo
        [HttpPost("{id:int}/generar-codigo")]
        public async Task<ActionResult<CodigoConfirmacionResponseDTO>> GenerarCodigoParaPedido(int id)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();

            if (pedido.PagoEstado != EstadoPago.PENDIENTE || (pedido.Total ?? 0m) <= 0m)
                return BadRequest(new { message = "Pedido no listo para generar código." });

            var email =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;

            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var dto = await _codigos.GenerarAsync(email);
            return Ok(dto); // En producción: envía por correo y no lo devuelvas.
        }

        // POST /api/pedido/123/confirmar
        [HttpPost("{id:int}/confirmar")]
        public async Task<IActionResult> ConfirmarPago(int id, [FromBody] CodigoValidarDTO dto)
        {
            var email =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;

            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var valid = await _codigos.ValidarAsync(email, dto.Codigo);
            if (!valid) return BadRequest(new { message = "Código inválido o expirado." });

            var ok = await _pedidos.MarcarPagadoAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}
