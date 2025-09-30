using ClickMart.DTOs.ResenaDTOs;

namespace ClickMart.Interfaces
{
    public interface IResenaService
    {
        Task<List<ResenaResponseDTO>> GetAllAsync();
        Task<ResenaResponseDTO?> GetByIdAsync(int id);
        Task<ResenaResponseDTO> CreateAsync(ResenaCreateDTO dto);
        Task<bool> UpdateAsync(int id, ResenaUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<List<ResenaResponseDTO>> GetByProductoAsync(int productoId);
        Task<int> CountByUsuarioAsync(int usuarioId);
        Task<int> CountByProductoAsync(int productoId);
    }
}