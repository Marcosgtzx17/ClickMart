using ClickMart.DTOs.DistribuidorDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class DistribuidorService : IDistribuidorService
    {
        private readonly IDistribuidorRepository _repo;

        public DistribuidorService(IDistribuidorRepository repo) => _repo = repo;

        public async Task<List<DistribuidorResponseDTO>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(Map).ToList();
        }

        public async Task<DistribuidorResponseDTO?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity is null ? null : Map(entity);
        }

        public async Task<DistribuidorResponseDTO?> GetByGmailAsync(string gmail)
        {
            var entity = await _repo.GetByGmailAsync(gmail.Trim());
            return entity is null ? null : Map(entity);
        }

        public async Task<DistribuidorResponseDTO> CreateAsync(DistribuidorCreateDTO dto)
        {
            var gmail = (dto.Gmail ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(gmail))
            {
                var inUse = await _repo.GmailInUseAsync(gmail);
                if (inUse) throw new InvalidOperationException("El gmail ya está en uso por otro distribuidor.");
            }

            var entity = new Distribuidor
            {
                Nombre = (dto.Nombre ?? "").Trim(),
                Direccion = (dto.Direccion ?? "").Trim(),
                Telefono = (dto.Telefono ?? "").Trim(),
                Gmail = gmail,
                Descripcion = (dto.Descripcion ?? "").Trim(),
                FechaRegistro = dto.FechaRegistro == default ? DateTime.UtcNow.Date : dto.FechaRegistro.Date
            };

            var created = await _repo.AddAsync(entity);
            return Map(created);
        }

        public async Task<bool> UpdateAsync(int id, DistribuidorUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            var gmail = (dto.Gmail ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(gmail))
            {
                var inUse = await _repo.GmailInUseAsync(gmail, excludeId: id);
                if (inUse) throw new InvalidOperationException("El gmail ya está en uso por otro distribuidor.");
            }

            current.Nombre = (dto.Nombre ?? "").Trim();
            current.Direccion = (dto.Direccion ?? "").Trim();
            current.Telefono = (dto.Telefono ?? "").Trim();
            current.Gmail = gmail;
            current.Descripcion = (dto.Descripcion ?? "").Trim();
            current.FechaRegistro = dto.FechaRegistro == default ? current.FechaRegistro : dto.FechaRegistro.Date;

            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        private static DistribuidorResponseDTO Map(Distribuidor d) => new()
        {
            DistribuidorId = d.DistribuidorId,
            Nombre = d.Nombre,
            Direccion = d.Direccion,
            Telefono = d.Telefono,
            Gmail = d.Gmail,
            Descripcion = d.Descripcion,
            FechaRegistro = d.FechaRegistro
        };
    }
}
