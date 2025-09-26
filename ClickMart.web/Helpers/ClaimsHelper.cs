using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ClickMart.web.DTOs.UsuarioDTOs;

namespace ClickMart.web.Helpers
{
    public static class ClaimsHelper
    {
        public static ClaimsPrincipal BuildPrincipalFromAuth(UsuarioRespuestaDTO auth)
        {
            var rolOriginal = (auth.Rol ?? "").Trim();
            var rolNorm = rolOriginal.ToLowerInvariant(); // "administrador", "admin", etc.

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,  auth.Nombre ?? string.Empty),
                new Claim(ClaimTypes.Email, auth.Email  ?? string.Empty),

                // Mantén el rol original…
                new Claim(ClaimTypes.Role, rolOriginal),

                // …y añade uno normalizado para que IsInRole/nuestros checks no fallen.
                new Claim(ClaimTypes.Role, rolNorm),

                // Alias útil si en algún sitio lees "rol"
                new Claim("rol", rolNorm),

                // token de la API
                new Claim("token", auth.Token ?? string.Empty),
            };

            var identity = new ClaimsIdentity(claims, "AuthCookie");
            return new ClaimsPrincipal(identity);
        }

        public static string? GetToken(ClaimsPrincipal user) =>
            user?.Claims?.FirstOrDefault(c => c.Type == "token")?.Value;
    


        /// <summary>
        /// Devuelve true solo si PODEMOS leer el JWT y su exp ya pasó (con holgura).
        /// Si no podemos leer, devolvemos false para no romper el flujo.
        /// </summary>
        public static bool IsJwtExpired(string? jwt, int clockSkewSeconds = 90)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return true;  // token vacío sí es inválido

            try
            {
                // Por si viene con "Bearer "
                var raw = jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                          ? jwt.Substring(7).Trim()
                          : jwt.Trim();

                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(raw);

                // Preferimos 'exp' (UNIX seconds). Si no, caemos a ValidTo.
                var expClaim = token.Claims.FirstOrDefault(c => c.Type is "exp")?.Value;
                DateTimeOffset expUtc;

                if (!string.IsNullOrWhiteSpace(expClaim) && long.TryParse(expClaim, out var expSeconds))
                {
                    expUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                }
                else
                {
                    // ValidTo es UTC
                    expUtc = token.ValidTo == default ? DateTimeOffset.MaxValue : token.ValidTo;
                }

                // Holgura para relojes desincronizados
                var now = DateTimeOffset.UtcNow.AddSeconds(clockSkewSeconds);
                return now >= expUtc;
            }
            catch
            {
                // Si no pudimos leerlo, no lo consideramos expirado para no bloquear.
                // (Si realmente está inválido, la API devolverá 401 y lo manejamos allí.)
                return false;
            }
        }
    }
}
