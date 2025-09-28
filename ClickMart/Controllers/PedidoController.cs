using ClickMart.DTOs.CodigoConfirmacionDTOs;
using ClickMart.DTOs.PedidoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClickMart.Utils;
using ClaimsEx = ClickMart.Utils.ClaimsExtensions;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Acepta todas las variantes de admin para no quedar fuera por un alias
    [Authorize(Roles = "Cliente,Admin,Administrador,Administrator")]
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
        [Authorize(Roles = "Admin,Administrador,Administrator")]
        [ProducesResponseType(typeof(List<PedidoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PedidoResponseDTO>>> GetAll() =>
            Ok(await _pedidos.GetAllAsync());

        // Mis pedidos (Cliente). Si entra un Admin, verá los suyos (por uid)
        [HttpGet("mios")]
        [ProducesResponseType(typeof(List<PedidoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<PedidoResponseDTO>>> GetMine()
        {
            var uid = User.GetUserId();
            if (uid is null) return Unauthorized();
            return Ok(await _pedidos.GetByUsuarioAsync(uid.Value));
        }

        // GET /api/pedido/{id}  -> dueño o Admin (blindado contra 500)
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PedidoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PedidoResponseDTO>> GetById(int id)
        {
            try
            {
                var dto = await _pedidos.GetByIdAsync(id);
                if (dto is null) return NotFound();

                if (!User.IsAdmin())
                {
                    var uid = User.GetUserId();
                    if (uid is null || dto.UsuarioId != uid.Value) return Forbid();
                }

                return Ok(dto);
            }
            catch
            {
                // Evita 500 “crudo” sin detalle
                return Problem(title: "Error obteniendo el pedido.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // POST /api/pedido
        [HttpPost]
        [ProducesResponseType(typeof(PedidoResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PedidoResponseDTO>> Create([FromBody] PedidoCreateDTO dto)
        {
            // Cliente: ignora UsuarioId y fuerza el del token
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

        // PUT /api/pedido/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(int id, [FromBody] PedidoUpdateDTO dto)
        {
            var current = await _pedidos.GetByIdAsync(id);
            if (current is null) return NotFound();

            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null || uid.Value != current.UsuarioId) return Forbid();
            }

            var ok = await _pedidos.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/pedido/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(int id)
        {
            var current = await _pedidos.GetByIdAsync(id);
            if (current is null) return NotFound();

            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null || uid.Value != current.UsuarioId) return Forbid();
            }

            var ok = await _pedidos.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // POST /api/pedido/{id}/recalcular-total
        [HttpPost("{id:int}/recalcular-total")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RecalcularTotal(int id)
        {
            var current = await _pedidos.GetByIdAsync(id);
            if (current is null) return NotFound();

            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null || uid.Value != current.UsuarioId) return Forbid();
            }

            var total = await _pedidos.RecalcularTotalAsync(id);
            return Ok(new { pedidoId = id, total });
        }

        // POST /api/pedido/{id}/generar-codigo
        [HttpPost("{id:int}/generar-codigo")]
        [ProducesResponseType(typeof(CodigoConfirmacionResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CodigoConfirmacionResponseDTO>> GenerarCodigoParaPedido(int id)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();

            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null || uid.Value != pedido.UsuarioId) return Forbid();
            }

            if (pedido.PagoEstado != EstadoPago.PENDIENTE || (pedido.Total ?? 0m) <= 0m)
                return BadRequest(new { message = "Pedido no listo para generar código." });

            var email = ClaimsEx.GetEmail(User);
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "No se encontró el correo en el token." });

            var dto = await _codigos.GenerarAsync(email);
            return Ok(dto);
        }

        // POST /api/pedido/{id}/confirmar
        [HttpPost("{id:int}/confirmar")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ConfirmarPago(int id, [FromBody] CodigoValidarDTO dto)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();

            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null || uid.Value != pedido.UsuarioId) return Forbid();
            }

            var email = ClaimsEx.GetEmail(User);
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "No se encontró el correo en el token." });

            var valid = await _codigos.ValidarAsync(email, dto.Codigo);
            if (!valid) return BadRequest(new { message = "Código inválido o expirado." });

            var ok = await _pedidos.MarcarPagadoAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // GET /api/pedido/{id}/factura
        [HttpGet("{id:int}/factura")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DescargarFactura(int id)
        {
            var pedido = await _pedidos.GetByIdAsync(id);
            if (pedido is null) return NotFound();

            if (!User.IsAdmin())
            {
                var uid = User.GetUserId();
                if (uid is null || uid.Value != pedido.UsuarioId) return Forbid();
            }

            var pdf = await _facturas.GenerarFacturaPdfAsync(id);
            return pdf is null
                ? NotFound()
                : File(pdf, "application/pdf", $"Factura_{id}.pdf");
        }
    }
}
