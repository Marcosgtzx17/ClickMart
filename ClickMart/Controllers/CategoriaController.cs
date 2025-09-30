// Api/Controllers/CategoriaController.cs
using ClickMart.DTOs.CategoriaDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaProductoService _svc;
        private readonly IProductoService _productos;

        public CategoriaController(ICategoriaProductoService svc, IProductoService productos)
        {
            _svc = svc;
            _productos = productos;
        }

        // Lectura libre (ajusta si quieres proteger)
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

        // Mutaciones solo admin (acepta varios alias)
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

        // === Delete con protección de FK (409) ===
        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _svc.GetByIdAsync(id);
            if (cat is null) return NotFound();

            // 1) Bloqueo proactivo: ¿hay productos en esta categoría?
            var productosUsando = await _productos.CountByCategoriaAsync(id);
            if (productosUsando > 0)
            {
                return Conflict(new
                {
                    message = "No se puede eliminar la categoría porque está en uso.",
                    detalles = new { productos = productosUsando },
                    sugerencia = "Reasigna o elimina los productos antes de eliminar la categoría."
                });
            }

            // 2) Fallback por si otra FK te explota
            try
            {
                var ok = await _svc.DeleteAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (DbUpdateException dbex)
            {
                return Conflict(new
                {
                    message = "No se puede eliminar la categoría debido a referencias existentes.",
                    error = dbex.InnerException?.Message ?? dbex.Message
                });
            }
        }
    }
}