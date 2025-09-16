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


        [HttpGet]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetAll() => Ok(await _svc.GetAllAsync());


        [HttpGet("pedido/{pedidoId:int}")]
        public async Task<ActionResult<List<DetallePedidoResponseDTO>>> GetByPedido(int pedidoId) => Ok(await _svc.GetByPedidoAsync(pedidoId));


        [HttpGet("{id:int}")]
        public async Task<ActionResult<DetallePedidoResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }


        [HttpPost]
        public async Task<ActionResult<DetallePedidoResponseDTO>> Create([FromBody] DetallePedidoCreateDTO dto)
        {
            var created = await _svc.CreateAsync(dto);
            // Recalcular total del pedido
            await _pedidoSvc.RecalcularTotalAsync(dto.IdPedido);
            return CreatedAtAction(nameof(GetById), new { id = created.DetalleId }, created);
        }


        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DetallePedidoUpdateDTO dto)
        {
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var byId = await _svc.GetByIdAsync(id);
            var ok = await _svc.DeleteAsync(id);
            if (ok && byId != null)
            {
                await _pedidoSvc.RecalcularTotalAsync(byId.IdPedido);
            }
            return ok ? NoContent() : NotFound();
        }
    }
}