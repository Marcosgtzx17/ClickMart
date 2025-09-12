using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // estos endpoints no requieren token
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>Registro de usuario (devuelve JWT)</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UsuarioRespuestaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UsuarioRespuestaDTO>> Registrar([FromBody] UsuarioRegistroDTO dto)
        {
            var result = await _authService.RegistrarAsync(dto);
            if (result is null)
                return Conflict(new { message = "El email ya está registrado." });

            return Ok(result);
        }

        /// <summary>Login (devuelve JWT)</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UsuarioRespuestaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UsuarioRespuestaDTO>> Login([FromBody] UsuarioLoginDTO dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (result is null)
                return Unauthorized(new { message = "Credenciales inválidas." });

            return Ok(result);
        }
    }
}
