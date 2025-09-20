using ClickMart.DTOs.DistribuidorDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize (Roles ="administradores")] // Si quieres testear en Swagger sin JWT, comenta esto temporalmente.
    public class DistribuidorController : ControllerBase
    {
        private readonly IDistribuidorService _service;

        public DistribuidorController(IDistribuidorService service) => _service = service;

        // GET: api/Distribuidor
        [HttpGet]
        [ProducesResponseType(typeof(List<DistribuidorResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        // GET: api/Distribuidor/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DistribuidorResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // GET: api/Distribuidor/by-gmail?gmail=foo@bar.com
        [HttpGet("by-gmail")]
        [ProducesResponseType(typeof(DistribuidorResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByGmail([FromQuery] string gmail)
        {
            var dto = await _service.GetByGmailAsync(gmail);
            return dto is null ? NotFound() : Ok(dto);
        }

        // POST: api/Distribuidor
        [HttpPost]
        [ProducesResponseType(typeof(DistribuidorResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] DistribuidorCreateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.DistribuidorId }, created);
        }

        // PUT: api/Distribuidor/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] DistribuidorUpdateDTO dto)
        {
            var ok = await _service.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE: api/Distribuidor/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}
