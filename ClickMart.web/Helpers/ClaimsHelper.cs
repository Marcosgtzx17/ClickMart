using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ClickMart.web.DTOs.UsuarioDTOs;

namespace ClickMart.web.Helpers
{
    public static class ClaimsHelper
    {
        public static ClaimsPrincipal BuildPrincipalFromAuth(UsuarioRespuestaDTO auth)
        {
            // Normaliza datos con defaults sanos
            var usuarioId = auth.UsuarioId;
            var nombre = string.IsNullOrWhiteSpace(auth.Nombre) ? auth.Email : auth.Nombre;
            var rol = string.IsNullOrWhiteSpace(auth.Rol) ? "Cliente" : auth.Rol;
            var token = auth.Token ?? string.Empty;

            var claims = new List<Claim>
            {
                // Identidad básica
                new Claim(ClaimTypes.Name,  nombre ?? string.Empty),
                new Claim(ClaimTypes.Email, auth.Email  ?? string.Empty),

                // IDs en varias claves para máxima compatibilidad
                new Claim("uid", usuarioId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim("sub", usuarioId.ToString()),

                // Rol en distintos tipos de claim
                new Claim(ClaimTypes.Role, rol),  // [Authorize(Roles=...)]
                new Claim("role",  rol),
                new Claim("roles", rol),
                new Claim("rol",   rol),

                // Token JWT para tus services que llaman la API
                new Claim("token", token),
            };

            var identity = new ClaimsIdentity(claims, "AuthCookie");
            return new ClaimsPrincipal(identity);
        }

        public static string? GetToken(ClaimsPrincipal user) =>
            user?.Claims?.FirstOrDefault(c => c.Type == "token")?.Value;

        /// Devuelve true si el JWT está expirado; en caso de duda, no bloquea.
        public static bool IsJwtExpired(string? jwt, int clockSkewSeconds = 90)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return true;  // vacío = inválido

            try
            {
                var raw = jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                          ? jwt[7..].Trim()
                          : jwt.Trim();

                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(raw);

                var expClaim = token.Claims.FirstOrDefault(c => c.Type is "exp")?.Value;
                DateTimeOffset expUtc;

                if (!string.IsNullOrWhiteSpace(expClaim) && long.TryParse(expClaim, out var expSeconds))
                    expUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                else
                    expUtc = token.ValidTo == default ? DateTimeOffset.MaxValue : token.ValidTo;

                var now = DateTimeOffset.UtcNow.AddSeconds(clockSkewSeconds);
                return now >= expUtc;
            }
            catch
            {
                // Si no pudimos leerlo, no lo consideramos expirado: la API decidirá
                return false;
            }
        }
    }
}
