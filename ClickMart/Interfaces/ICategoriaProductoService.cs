// Interfaces/ICategoriaProductoService.cs
using ClickMart.DTOs.CategoriaDTOs;

namespace ClickMart.Interfaces
{
    public interface ICategoriaProductoService
    {
        Task<List<CategoriaResponseDTO>> GetAllAsync();
        Task<CategoriaResponseDTO?> GetByIdAsync(int id);
        Task<CategoriaResponseDTO> CreateAsync(CategoriaCreateDTO dto);
        Task<bool> UpdateAsync(int id, CategoriaUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}

