// Servicios/RolService.cs
using ClickMart.DTOs.RolDTOs;
using ClickMart.DTOs.RolDTOs.ClickMart.DTOs.RolDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class RolService : IRolService
    {
        private readonly IRolRepository _repo;
        public RolService(IRolRepository repo) => _repo = repo;

        public async Task<List<RolResponseDTO>> GetAllAsync() =>
            (await _repo.GetAllAsync())
            .Select(x => new RolResponseDTO { RolId = x.RolId, Nombre = x.Nombre })
            .ToList();

        public async Task<RolResponseDTO?> GetByIdAsync(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            return r is null ? null : new RolResponseDTO { RolId = r.RolId, Nombre = r.Nombre };
        }

        public async Task<RolResponseDTO> CreateAsync(RolCreateDTO dto)
        {
            // (opcional) validar nombre único
            var exists = await _repo.GetByNameAsync(dto.Nombre.Trim());
            if (exists != null) throw new InvalidOperationException("El rol ya existe.");

            var entity = new Rol { Nombre = dto.Nombre.Trim() };
            var saved = await _repo.AddAsync(entity);
            return new RolResponseDTO { RolId = saved.RolId, Nombre = saved.Nombre };
        }

        public async Task<bool> UpdateAsync(int id, RolUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            current.Nombre = dto.Nombre.Trim();
            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}