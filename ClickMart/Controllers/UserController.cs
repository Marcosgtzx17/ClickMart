using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "adminitrador")] // requiere token válido
    public class UserController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioService _svc;

        public UserController(IUsuarioService svc, IUsuarioRepository usuarioRepository)
        {
            _svc = svc;
            _usuarioRepository = usuarioRepository;
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
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/user/{id}
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
