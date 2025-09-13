
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class CategoriaProductoRepository : ICategoriaProductoRepository
    {
        private readonly AppDbContext _ctx;
        public CategoriaProductoRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<CategoriaProducto>> GetAllAsync()
            => await _ctx.Set<CategoriaProducto>().AsNoTracking().ToListAsync();

        public Task<CategoriaProducto?> GetByIdAsync(int id)
            => _ctx.Set<CategoriaProducto>().FindAsync(id).AsTask();

        public async Task<CategoriaProducto> AddAsync(CategoriaProducto entity)
        {
            _ctx.Set<CategoriaProducto>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(CategoriaProducto entity)
        {
            _ctx.Set<CategoriaProducto>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<CategoriaProducto>().FindAsync(id);
            if (existing is null) return false;
            _ctx.Set<CategoriaProducto>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}

