using ClickMart.Entidades;

namespace ClickMart.Interfaces
{
    public interface IProductoRepository
    {
        Task<List<Productos>> GetAllAsync();
        Task<Productos?> GetByIdAsync(int id);
        Task<Productos> AddAsync(Productos entity);
        Task<bool> UpdateAsync(Productos entity);
        Task<bool> DeleteAsync(int id);
    }
}