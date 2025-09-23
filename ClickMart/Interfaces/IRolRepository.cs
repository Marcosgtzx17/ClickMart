// Interfaces/IRolRepository.cs
using ClickMart.Entidades;

namespace ClickMart.Interfaces
{
    public interface IRolRepository
    {
        Task<List<Rol>> GetAllAsync();
        Task<Rol?> GetByIdAsync(int id);
        Task<Rol?> GetByNameAsync(string nombre);
        Task<Rol> AddAsync(Rol entity);
        Task<bool> UpdateAsync(Rol entity);
        Task<bool> DeleteAsync(int id);


    }
}