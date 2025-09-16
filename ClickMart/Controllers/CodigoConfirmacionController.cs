using ClickMart.DTOs.CodigoConfirmacionDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CodigoController : ControllerBase
    {
        private readonly ICodigoConfirmacionService _svc;
        public CodigoController(ICodigoConfirmacionService svc) => _svc = svc;


        [HttpPost("generar")]
        public async Task<ActionResult<CodigoConfirmacionResponseDTO>> Generar()
        {
            string email = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var dto = await _svc.GenerarAsync(email);
            return Ok(dto); // en prod, no devuelvas el código; envíalo por email
        }


        [HttpPost("validar")]
        public async Task<IActionResult> Validar([FromBody] CodigoValidarDTO body)
        {
            string email = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var ok = await _svc.ValidarAsync(email, body.Codigo);
            return ok ? NoContent() : BadRequest(new { message = "Código inválido o expirado." });
        }
    }
}