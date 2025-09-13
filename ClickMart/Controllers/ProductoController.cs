using ClickMart.DTOs.ProductoDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _svc;
        public ProductoController(IProductoService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<List<ProductoResponseDTO>>> GetAll() =>
            Ok(await _svc.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductoResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }

        [HttpPost]
        public async Task<ActionResult<ProductoResponseDTO>> Create([FromBody] ProductoCreateDTO dto)
        {
            var saved = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = saved.ProductoId }, saved);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDTO dto)
        {
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}