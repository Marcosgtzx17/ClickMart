using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // requiere token válido
    public class UserController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UserController(IUsuarioRepository usuarioRepository)
        {
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
    }
}

