// Controllers/CategoriaController.cs
using ClickMart.DTOs.CategoriaDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles ="administradores")]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaProductoService _svc;
        public CategoriaController(ICategoriaProductoService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<List<CategoriaResponseDTO>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoriaResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }

        [HttpPost]
        public async Task<ActionResult<CategoriaResponseDTO>> Create([FromBody] CategoriaCreateDTO dto)
        {
            var saved = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = saved.CategoriaId }, saved);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoriaUpdateDTO dto)
            => (await _svc.UpdateAsync(id, dto)) ? NoContent() : NotFound();

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => (await _svc.DeleteAsync(id)) ? NoContent() : NotFound();
    }
}
