// Controllers/CategoriaController.cs
using ClickMart.DTOs.CategoriaDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaProductoService _svc;
        public CategoriaController(ICategoriaProductoService svc) => _svc = svc;

        // >>> Lectura SIN rol (puede ser pública; si prefieres autenticado, cambia a [Authorize] a secas)
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<CategoriaResponseDTO>>> GetAll()
            => Ok(await _svc.GetAllAsync());

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoriaResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }

        // >>> Mutaciones SOLO admin (acepta todos los alias de rol)
        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        [HttpPost]
        public async Task<ActionResult<CategoriaResponseDTO>> Create([FromBody] CategoriaCreateDTO dto)
        {
            var saved = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = saved.CategoriaId }, saved);
        }

        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoriaUpdateDTO dto)
            => (await _svc.UpdateAsync(id, dto)) ? NoContent() : NotFound();

        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
            => (await _svc.DeleteAsync(id)) ? NoContent() : NotFound();
    }
}
