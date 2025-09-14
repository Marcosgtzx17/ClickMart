using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Interfaces
{
    public interface IResenaRepository
    {
        Task<List<Resena>> GetAllAsync();
        Task<Resena?> GetByIdAsync(int id);
        Task<Resena> AddAsync(Resena entity);
        Task<bool> UpdateAsync(Resena entity);
        Task<bool> DeleteAsync(int id);
    }
}
