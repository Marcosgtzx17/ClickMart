
using ClickMart.DTOs.CategoriaDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class CategoriaProductoService : ICategoriaProductoService
    {
        private readonly ICategoriaProductoRepository _repo;
        public CategoriaProductoService(ICategoriaProductoRepository repo) => _repo = repo;

        public async Task<List<CategoriaResponseDTO>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(x => new CategoriaResponseDTO
            {
                CategoriaId = x.CategoriaId,
                Nombre = x.Nombre
            }).ToList();

        public async Task<CategoriaResponseDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            return x is null ? null : new CategoriaResponseDTO
            {
                CategoriaId = x.CategoriaId,
                Nombre = x.Nombre
            };
        }

        public async Task<CategoriaResponseDTO> CreateAsync(CategoriaCreateDTO dto)
        {
            var entity = new CategoriaProducto { Nombre = dto.Nombre.Trim() };
            var saved = await _repo.AddAsync(entity);
            return new CategoriaResponseDTO { CategoriaId = saved.CategoriaId, Nombre = saved.Nombre };
        }

        public async Task<bool> UpdateAsync(int id, CategoriaUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;
            current.Nombre = dto.Nombre.Trim();
            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}
