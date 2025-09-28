using ClickMart.DTOs.DetallePedidoDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClickMart.Utils;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Cliente,Admin,Administrador,Administrator")]
    public class DetallePedidoController : ControllerBase
    {
        private readonly IDetallePedidoService _svc;
        private readonly IPedidoService _pedidoSvc;

        public DetallePedidoController(IDetallePedidoService svc, IPedidoService pedidoSvc)
        {
            _svc = svc;
            _pedidoSvc = pedidoSvc;
        }

        // GET /api/DetallePedido  -> SOLO ADMIN
        [HttpGet]
        [Authorize(Roles = "Admin,Administrador,Administrator")]
        [ProducesResponseType(typeof(List<DetallePedidoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET /api/DetallePedido/pedido/{pedidoId}  -> dueño o Admin
        [HttpGet("pedido/{pedidoId:int}")]
        [ProducesResponseType(typeof(List<DetallePedidoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetByPedido(int pedidoId)
        {
            if (!User.IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(pedidoId);
                if (pedido is null) return NotFound();

                var uid = User.GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            return Ok(await _svc.GetByPedidoAsync(pedidoId));
        }

        // GET /api/DetallePedido/{id}  -> dueño o Admin
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DetallePedidoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DetallePedidoResponseDTO>> GetById(int id)
        {
            var dto = await _svc.GetByIdAsync(id);
            if (dto is null) return NotFound();

            if (!User.IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(dto.IdPedido);
                if (pedido is null) return NotFound();

                var uid = User.GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            return Ok(dto);
        }

        // POST /api/DetallePedido  -> dueño o Admin
        [HttpPost]
        [ProducesResponseType(typeof(DetallePedidoResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DetallePedidoResponseDTO>> Create([FromBody] DetallePedidoCreateDTO dto)
        {
            if (!User.IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(dto.IdPedido);
                if (pedido is null) return NotFound();

                var uid = User.GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.DetalleId }, created);
        }

        // PUT /api/DetallePedido/{id}  -> dueño o Admin
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(int id, [FromBody] DetallePedidoUpdateDTO dto)
        {
            var det = await _svc.GetByIdAsync(id);
            if (det is null) return NotFound();

            if (!User.IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(det.IdPedido);
                if (pedido is null) return NotFound();

                var uid = User.GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/DetallePedido/{id}  -> dueño o Admin
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(int id)
        {
            var det = await _svc.GetByIdAsync(id);
            if (det is null) return NotFound();

            if (!User.IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(det.IdPedido);
                if (pedido is null) return NotFound();

                var uid = User.GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // POST /api/DetallePedido/pedido/{pedidoId}/recalcular  -> dueño o Admin
        [HttpPost("pedido/{pedidoId:int}/recalcular")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RecalcularTotalPedido(int pedidoId)
        {
            if (!User.IsAdmin())
            {
                var pedido = await _pedidoSvc.GetByIdAsync(pedidoId);
                if (pedido is null) return NotFound();

                var uid = User.GetUserId();
                if (uid is null || pedido.UsuarioId != uid.Value) return Forbid();
            }

            var total = await _pedidoSvc.RecalcularTotalAsync(pedidoId);
            return Ok(new { pedidoId, total });
        }
    }
}
