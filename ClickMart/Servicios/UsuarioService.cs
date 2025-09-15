using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repo;
        public UsuarioService(IUsuarioRepository repo) => _repo = repo;

        public async Task<UsuarioListadoDTO?> GetByIdAsync(int id)
        {
            var u = await _repo.GetByIdAsync(id);
            return u is null
                ? null
                : new UsuarioListadoDTO
                {
                    UsuarioId = u.UsuarioId,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Rol = u.Rol?.Nombre ?? "Sin Rol"
                };
        }
        public async Task<UsuarioListadoDTO> CreateAsync(UsuarioCreateDTO dto)
        {
            // Validar email único
            var exists = await _repo.GetByEmailAsync(dto.Email);
            if (exists != null)
                throw new InvalidOperationException("El email ya está registrado.");

            // Mapear entidad (hash del password aunque el dto se llame PasswordHash)
            var user = new Usuario
            {
                Nombre = (dto.Nombre ?? string.Empty).Trim(),
                Direccion = (dto.Direccion ?? string.Empty).Trim(),
                Telefono = (dto.Telefono ?? string.Empty).Trim(),
                Email = (dto.Email ?? string.Empty).Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash ?? string.Empty),
                RolId = dto.RolId
            };

            await _repo.AddAsync(user);

            // Cargar con Rol para responder
            var loaded = await _repo.GetByIdAsync(user.UsuarioId);
            return new UsuarioListadoDTO
            {
                UsuarioId = loaded!.UsuarioId,
                Nombre = loaded.Nombre,
                Email = loaded.Email,
                Rol = loaded.Rol?.Nombre ?? "Sin Rol"
            };
        }


        public async Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto)
        {
            var u = await _repo.GetByIdAsync(id);
            if (u is null) return false;

            // Validar email único si cambia
            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !dto.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _repo.GetByEmailAsync(dto.Email);
                if (exists != null) return false; // puedes manejar como 409 en el controller
            }

            u.Nombre = dto.Nombre?.Trim() ?? u.Nombre;
            u.Direccion = dto.Direccion?.Trim() ?? u.Direccion;
            u.Telefono = dto.Telefono?.Trim() ?? u.Telefono;
            u.Email = dto.Email?.Trim() ?? u.Email;

            return await _repo.UpdateAsync(u);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}

