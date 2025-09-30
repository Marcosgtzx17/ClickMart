using ClickMart.DTOs.DistribuidorDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ajusta roles si quieres: (Roles = "Admin,Administrador,Administrator")
    public class DistribuidorController : ControllerBase
    {
        private readonly IDistribuidorService _svc;
        private readonly IProductoService _productos;

        public DistribuidorController(IDistribuidorService svc, IProductoService productos)
        {
            _svc = svc;
            _productos = productos;
        }

        // GET: api/Distribuidor
        [HttpGet]
        [ProducesResponseType(typeof(List<DistribuidorResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());

        // GET: api/Distribuidor/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DistribuidorResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _svc.GetByIdAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // GET: api/Distribuidor/by-gmail?gmail=foo@bar.com
        [HttpGet("by-gmail")]
        [ProducesResponseType(typeof(DistribuidorResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByGmail([FromQuery] string gmail)
        {
            var dto = await _svc.GetByGmailAsync(gmail);
            return dto is null ? NotFound() : Ok(dto);
        }

        // POST: api/Distribuidor
        [HttpPost]
        [ProducesResponseType(typeof(DistribuidorResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] DistribuidorCreateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.DistribuidorId }, created);
        }

        // PUT: api/Distribuidor/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] DistribuidorUpdateDTO dto)
        {
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE: api/Distribuidor/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var dist = await _svc.GetByIdAsync(id);
            if (dist is null) return NotFound();

            // Bloqueo proactivo: ¿está referenciado?
            var productosUsando = await _productos.CountByDistribuidorAsync(id);
            if (productosUsando > 0)
            {
                return Conflict(new
                {
                    message = "No se puede eliminar el distribuidor porque está en uso.",
                    detalles = new { productos = productosUsando },
                    sugerencia = "Reasigna o elimina los productos antes de eliminar el distribuidor."
                });
            }

            try
            {
                var ok = await _svc.DeleteAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (DbUpdateException dbex)
            {
                // Airbag por si otra FK te explota o hay carrera
                return Conflict(new
                {
                    message = "No se puede eliminar el distribuidor debido a referencias existentes.",
                    error = dbex.InnerException?.Message ?? dbex.Message
                });
            }
        }
    }
}