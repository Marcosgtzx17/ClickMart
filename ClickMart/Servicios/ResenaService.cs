using ClickMart.DTOs.ResenaDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class ResenaService : IResenaService
    {
        private readonly IResenaRepository _repo;
        public ResenaService(IResenaRepository repo) => _repo = repo;

        public async Task<List<ResenaResponseDTO>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(Map).ToList();

        public async Task<ResenaResponseDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            return x is null ? null : Map(x);
        }

        // ===== NUEVO: reseñas por producto =====
        public async Task<List<ResenaResponseDTO>> GetByProductoAsync(int productoId)
        {
            var list = await _repo.GetByProductoAsync(productoId) ?? new List<Resena>();
            return list.Select(Map).ToList();
        }

        public async Task<ResenaResponseDTO> CreateAsync(ResenaCreateDTO dto)
        {
            var entity = new Resena
            {
                UsuarioId = dto.UsuarioId,
                ProductoId = dto.ProductoId,
                Calificacion = dto.Calificacion,
                Comentario = dto.Comentario ?? string.Empty,
                FechaResena = dto.FechaResena ?? DateTime.UtcNow
            };

            var saved = await _repo.AddAsync(entity);
            return Map(saved);
        }

        public async Task<bool> UpdateAsync(int id, ResenaUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            if (dto.Calificacion.HasValue) current.Calificacion = dto.Calificacion.Value;
            if (dto.Comentario != null) current.Comentario = dto.Comentario;
            if (dto.FechaResena.HasValue) current.FechaResena = dto.FechaResena.Value;

            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        private static ResenaResponseDTO Map(Resena r) => new()
        {
            ResenaId = r.ResenaId,
            UsuarioId = r.UsuarioId,
            ProductoId = r.ProductoId,
            Calificacion = r.Calificacion,
            Comentario = r.Comentario,
            FechaResena = r.FechaResena
        };
        public Task<int> CountByUsuarioAsync(int usuarioId)
            => _repo.CountByUsuarioAsync(usuarioId);
        public Task<int> CountByProductoAsync(int productoId)
        => _repo.CountByProductoAsync(productoId);
    }
}

