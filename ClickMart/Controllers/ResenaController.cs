// Controllers/ResenaController.cs
using ClickMart.Api.Controllers;
using ClickMart.DTOs.ResenaDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResenaController : ControllerBase
    {
        private readonly IResenaService _svc;
        public ResenaController(IResenaService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<List<ResenaResponseDTO>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResenaResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }

        [HttpPost]
        public async Task<ActionResult<ResenaResponseDTO>> Create([FromBody] ResenaCreateDTO dto)
        {
            var saved = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = saved.ResenaId }, saved);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ResenaUpdateDTO dto)
            => (await _svc.UpdateAsync(id, dto)) ? NoContent() : NotFound();

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => (await _svc.DeleteAsync(id)) ? NoContent() : NotFound();
    }
}