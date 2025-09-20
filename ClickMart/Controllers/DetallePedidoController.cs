using ClickMart.DTOs.DetallePedidoDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente,Admin")]
    public class DetallePedidoController : ControllerBase
    {
        private readonly IDetallePedidoService _svc;
        private readonly IPedidoService _pedidoSvc;

        public DetallePedidoController(IDetallePedidoService svc, IPedidoService pedidoSvc)
        {
            _svc = svc;
            _pedidoSvc = pedidoSvc;
        }

        // === Helpers locales (evitamos dependencia externa) ===
        private int? GetUserId()
        {
            var raw = User.FindFirst("uid")?.Value
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.Identity?.Name;
            return int.TryParse(raw, out var id) ? id : null;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        // GET /api/DetallePedido  -> SOLO ADMIN
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<DetallePedidoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET /api/DetallePedido/pedido/6  -> dueño del pedido o Admin
        [HttpGet("pedido/{pedidoId:int}")]
        [ProducesResponseType(typeof(List<DetallePedidoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetByPedido(int pedidoId)
        {
            if (!IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(pedidoId);
                if (pedido is null) return NotFound();
                var uid = GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            return Ok(await _svc.GetByPedidoAsync(pedidoId));
        }

        // GET /api/DetallePedido/12  -> dueño del pedido o Admin
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DetallePedidoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DetallePedidoResponseDTO>> GetById(int id)
        {
            var dto = await _svc.GetByIdAsync(id);
            if (dto is null) return NotFound();

            if (!IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(dto.IdPedido);
                if (pedido is null) return NotFound();
                var uid = GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            return Ok(dto);
        }

        // POST /api/DetallePedido  -> dueño del pedido o Admin
        [HttpPost]
        [ProducesResponseType(typeof(DetallePedidoResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DetallePedidoResponseDTO>> Create([FromBody] DetallePedidoCreateDTO dto)
        {
            if (!IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(dto.IdPedido);
                if (pedido is null) return NotFound();
                var uid = GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.DetalleId }, created);
        }

        // PUT /api/DetallePedido/12  -> dueño del pedido o Admin
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(int id, [FromBody] DetallePedidoUpdateDTO dto)
        {
            // Primero determinamos a qué pedido pertenece el detalle
            var det = await _svc.GetByIdAsync(id);
            if (det is null) return NotFound();

            if (!IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(det.IdPedido);
                if (pedido is null) return NotFound();
                var uid = GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/DetallePedido/12  -> dueño del pedido o Admin
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(int id)
        {
            var det = await _svc.GetByIdAsync(id);
            if (det is null) return NotFound();

            if (!IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(det.IdPedido);
                if (pedido is null) return NotFound();
                var uid = GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // POST /api/DetallePedido/pedido/6/recalcular  -> dueño del pedido o Admin
        [HttpPost("pedido/{pedidoId:int}/recalcular")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RecalcularTotalPedido(int pedidoId)
        {
            if (!IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(pedidoId);
                if (pedido is null) return NotFound();
                var uid = GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var total = await _pedidoSvc.RecalcularTotalAsync(pedidoId);
            return Ok(new { pedidoId, total });
        }
    }
}
