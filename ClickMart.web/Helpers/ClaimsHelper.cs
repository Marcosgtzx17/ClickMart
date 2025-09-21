using System.Security.Claims;
using ClickMart.web.DTOs.UsuarioDTOs;

namespace ClickMart.web.Helpers
{
    public static class ClaimsHelper
    {
        public static ClaimsPrincipal BuildPrincipalFromAuth(UsuarioRespuestaDTO auth)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, auth.Nombre ?? string.Empty),
                new Claim(ClaimTypes.Email, auth.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(auth.Rol) ? "Usuario" : auth.Rol),
                new Claim("token", auth.Token ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "AuthCookie");
            return new ClaimsPrincipal(identity);
        }

        public static string? GetToken(ClaimsPrincipal user) =>
            user?.Claims?.FirstOrDefault(c => c.Type == "token")?.Value;
    }
}