using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClickMart.Repositorios
{
    public class ResenaRepository : IResenaRepository
    {
        private readonly AppDbContext _ctx;
        public ResenaRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<Resena>> GetAllAsync()
            => await _ctx.Set<Resena>().AsNoTracking().ToListAsync();

        public Task<Resena?> GetByIdAsync(int id)
            => _ctx.Set<Resena>().FindAsync(id).AsTask();

        // === NUEVO ===
        public async Task<List<Resena>> GetByProductoAsync(int productoId)
            => await _ctx.Set<Resena>()
                         .AsNoTracking()
                         .Where(r => r.ProductoId == productoId)
                         .ToListAsync();

        public async Task<Resena> AddAsync(Resena entity)
        {
            _ctx.Set<Resena>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Resena entity)
        {
            _ctx.Set<Resena>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<Resena>().FindAsync(id);
            if (existing is null) return false;
            _ctx.Set<Resena>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}
