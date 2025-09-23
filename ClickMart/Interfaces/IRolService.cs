// Interfaces/IRolService.cs
using ClickMart.DTOs.RolDTOs;
using ClickMart.DTOs.RolDTOs.ClickMart.DTOs.RolDTOs;

namespace ClickMart.Interfaces
{
    public interface IRolService
    {
        Task<List<RolResponseDTO>> GetAllAsync();
        Task<RolResponseDTO?> GetByIdAsync(int id);
        Task<RolResponseDTO> CreateAsync(RolCreateDTO dto);
        Task<bool> UpdateAsync(int id, RolUpdateDTO dto);
        Task<bool> DeleteAsync(int id);


    }
}