using ClickMart.Entidades;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClickMart.Interfaces
{
    public interface IResenaRepository
    {
        Task<List<Resena>> GetAllAsync();
        Task<Resena?> GetByIdAsync(int id);
        Task<List<Resena>> GetByProductoAsync(int productoId); // <-- NUEVO (entidades)
        Task<Resena> AddAsync(Resena entity);
        Task<bool> UpdateAsync(Resena entity);
        Task<bool> DeleteAsync(int id);
        Task<int> CountByUsuarioAsync(int usuarioId);
    }
}
