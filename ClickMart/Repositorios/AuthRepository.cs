using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClickMart.Repositorios
{
    public class AuthRepository : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IConfiguration _config;

        public AuthRepository(IUsuarioRepository usuarioRepo, IConfiguration config)
        {
            _usuarioRepo = usuarioRepo;
            _config = config;
        }

        // ===== Validaciones requeridas para HU-1003 y HU-1006 =====
        private static void ValidarRegistro(UsuarioRegistroDTO dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Nombre) ||
                string.IsNullOrWhiteSpace(dto.Direccion) ||
                string.IsNullOrWhiteSpace(dto.Telefono) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Complete todos los campos obligatorios");
            }
        }

        private static void ValidarLogin(UsuarioLoginDTO dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Complete todos los campos obligatorios");
            }
        }
        // ==========================================================

        public async Task<UsuarioRespuestaDTO?> RegistrarAsync(UsuarioRegistroDTO dto)
        {
            // Validación de campos obligatorios (HU-1003)
            ValidarRegistro(dto);

            // 1) Email único (HU-1002)
            var existing = await _usuarioRepo.GetByEmailAsync(dto.Email);
            if (existing != null) return null; // o devuelve Conflict

            // 2) Rol por defecto si no viene en el DTO
            var rolId = dto.RolId ?? 2;

            // 3) Construir entidad (hasheamos aquí)
            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Direccion = dto.Direccion.Trim(),
                Telefono = dto.Telefono.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RolId = rolId
            };

            await _usuarioRepo.AddAsync(usuario);

            // 4) Recargar con navegación Rol
            var loaded = await _usuarioRepo.GetByEmailAsync(usuario.Email);
            if (loaded == null) return null;

            // 5) Token
            var token = GenerarToken(loaded);

            return new UsuarioRespuestaDTO
            {
                UsuarioId = loaded.UsuarioId,
                Nombre = loaded.Nombre,
                Email = loaded.Email,
                Rol = loaded.Rol?.Nombre ?? "Usuario",
                Token = token
            };
        }

        public async Task<UsuarioRespuestaDTO?> LoginAsync(UsuarioLoginDTO dto)
        {
            // Validación de campos obligatorios (HU-1006)
            ValidarLogin(dto);

            var usuario = await _usuarioRepo.GetByEmailAsync(dto.Email);
            if (usuario == null) return null;

            // Comparar password texto vs hash almacenado (HU-1005)
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
                return null;

            return new UsuarioRespuestaDTO
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol?.Nombre ?? "Usuario",
                Token = GenerarToken(usuario)
            };
        }

        private string GenerarToken(Usuario usuario)
        {
            if (usuario.Rol == null)
                throw new InvalidOperationException("El usuario no tiene rol asignado.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Email ?? string.Empty),
                new Claim("rol", usuario.Rol.Nombre),
                new Claim("uid", usuario.UsuarioId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}