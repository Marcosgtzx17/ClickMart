using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // requiere token válido
    public class UserController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioService _svc;

        // Opcional: si tienes servicios para contar referencias
        private readonly IPedidoService? _pedidos;     // <- inyecta si existe
        private readonly IResenaService? _resenas;     // <- inyecta si existe

        public UserController(
            IUsuarioService svc,
            IUsuarioRepository usuarioRepository,
            IPedidoService? pedidos = null,
            IResenaService? resenas = null)
        {
            _svc = svc;
            _usuarioRepository = usuarioRepository;
            _pedidos = pedidos;
            _resenas = resenas;
        }

        /// <summary>Listado de usuarios (requiere rol/token)</summary>
        [HttpGet("usuarios")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _usuarioRepository.GetAllUsuariosAsync();
            return Ok(usuarios);
        }

        // POST /api/user  (crear usuario con rol elegido)
        [HttpPost]
        [ProducesResponseType(typeof(UsuarioListadoDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] UsuarioCreateDTO dto)
        {
            try
            {
                var created = await _svc.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.UsuarioId }, created);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // GET /api/user/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UsuarioListadoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UsuarioListadoDTO>> GetById(int id)
        {
            var result = await _svc.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        // PUT /api/user/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDTO dto)
        {
            try
            {
                var ok = await _svc.UpdateAsync(id, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE /api/user/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _svc.GetByIdAsync(id);
            if (user is null) return NotFound();

            // 1) Bloqueo proactivo: cuenta referencias antes de intentar borrar
            var pedidos = _pedidos is null ? 0 : await _pedidos.CountByUsuarioAsync(id);   // implementa en tu service/repo
            var resenas = _resenas is null ? 0 : await _resenas.CountByUsuarioAsync(id);   // opcional

            if ((pedidos + resenas) > 0)
            {
                return Conflict(new
                {
                    message = "No se puede eliminar el usuario porque tiene referencias.",
                    detalles = new
                    {
                        pedidos,
                        resenas
                    },
                    sugerencia = "Transfiere o elimina las referencias antes de eliminar al usuario."
                });
            }

            // 2) Fallback: por si otra FK truena al borrar
            try
            {
                var ok = await _svc.DeleteAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (DbUpdateException dbex)
            {
                return Conflict(new
                {
                    message = "No se puede eliminar el usuario debido a referencias existentes.",
                    detalles = new { pedidos, resenas },
                    error = dbex.InnerException?.Message ?? dbex.Message
                });
            }
        }
    }
}