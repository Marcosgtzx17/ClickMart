using ClickMart.DTOs.DistribuidorDTOs;

namespace ClickMart.Interfaces
{
    public interface IDistribuidorService
    {
        Task<List<DistribuidorResponseDTO>> GetAllAsync();
        Task<DistribuidorResponseDTO?> GetByIdAsync(int id);
        Task<DistribuidorResponseDTO?> GetByGmailAsync(string gmail);
        Task<DistribuidorResponseDTO> CreateAsync(DistribuidorCreateDTO dto);
        Task<bool> UpdateAsync(int id, DistribuidorUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
