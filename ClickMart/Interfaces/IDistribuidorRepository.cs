using ClickMart.DTOs.DistribuidorDTOs;
using ClickMart.Entidades;

namespace ClickMart.Interfaces
{
    public interface IDistribuidorRepository
    {
        Task<List<Distribuidor>> GetAllAsync();
        Task<Distribuidor?> GetByIdAsync(int id);
        Task<Distribuidor?> GetByGmailAsync(string gmail);
        Task<Distribuidor> AddAsync(Distribuidor entity);
        Task<bool> UpdateAsync(Distribuidor entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> GmailInUseAsync(string gmail, int? excludeId = null);

    }
}
