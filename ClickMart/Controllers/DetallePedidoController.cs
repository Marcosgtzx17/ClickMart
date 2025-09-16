using ClickMart.DTOs.DetallePedidoDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DetallePedidoController : ControllerBase
    {
        private readonly IDetallePedidoService _svc;
        private readonly IPedidoService _pedidoSvc;

        public DetallePedidoController(IDetallePedidoService svc, IPedidoService pedidoSvc)
        {
            _svc = svc;
            _pedidoSvc = pedidoSvc;
        }

        // GET /api/DetallePedido
        [HttpGet]
        [ProducesResponseType(typeof(List<DetallePedidoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET /api/DetallePedido/pedido/6
        [HttpGet("pedido/{pedidoId:int}")]
        [ProducesResponseType(typeof(List<DetallePedidoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetByPedido(int pedidoId)
            => Ok(await _svc.GetByPedidoAsync(pedidoId));

        // GET /api/DetallePedido/12
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DetallePedidoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DetallePedidoResponseDTO>> GetById(int id)
        {
            var dto = await _svc.GetByIdAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // POST /api/DetallePedido
        [HttpPost]
        [ProducesResponseType(typeof(DetallePedidoResponseDTO), StatusCodes.Status201Created)]
        public async Task<ActionResult<DetallePedidoResponseDTO>> Create([FromBody] DetallePedidoCreateDTO dto)
        {
            // El servicio crea el detalle y recalcula el total del pedido
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.DetalleId }, created);
        }

        // PUT /api/DetallePedido/12
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] DetallePedidoUpdateDTO dto)
        {
            // El servicio actualiza y recalcula el total del pedido
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/DetallePedido/12
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            // El servicio elimina y recalcula el total del pedido
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // Extra opcional: forzar recálculo del total desde controller (debug/ops)
        // POST /api/DetallePedido/pedido/6/recalcular
        [HttpPost("pedido/{pedidoId:int}/recalcular")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> RecalcularTotalPedido(int pedidoId)
        {
            var total = await _pedidoSvc.RecalcularTotalAsync(pedidoId);
            return Ok(new { pedidoId, total });
        }
    }
}
