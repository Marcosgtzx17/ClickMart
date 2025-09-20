// Controllers/RolController.cs
using ClickMart.DTOs.RolDTOs;
using ClickMart.DTOs.RolDTOs.ClickMart.DTOs.RolDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "administradores")]
    public class RolController : ControllerBase
    {
        private readonly IRolService _svc;
        public RolController(IRolService svc) => _svc = svc;

        // GET /api/rol
        [HttpGet]
        [ProducesResponseType(typeof(List<RolResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
            => Ok(await _svc.GetAllAsync());

        // GET /api/rol/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RolResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _svc.GetByIdAsync(id);
            return r is null ? NotFound() : Ok(r);
        }

        // POST /api/rol
        [HttpPost]
        [ProducesResponseType(typeof(RolResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] RolCreateDTO dto)
        {
            try
            {
                var saved = await _svc.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = saved.RolId }, saved);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("existe", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT /api/rol/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] RolUpdateDTO dto)
        {
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/rol/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}