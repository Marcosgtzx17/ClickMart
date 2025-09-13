using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly AppDbContext _ctx;
        public ProductoRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<Productos>> GetAllAsync() =>
            await _ctx.Set<Productos>().AsNoTracking().ToListAsync();

        public async Task<Productos?> GetByIdAsync(int id) =>
            await _ctx.Set<Productos>().FindAsync(id);

        public async Task<Productos> AddAsync(Productos entity)
        {
            _ctx.Set<Productos>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Productos entity)
        {
            _ctx.Set<Productos>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<Productos>().FindAsync(id);
            if (existing is null) return false;
            _ctx.Set<Productos>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}