using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class DistribuidorRepository : IDistribuidorRepository
    {
        private readonly AppDbContext _ctx;

        public DistribuidorRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<Distribuidor>> GetAllAsync()
            => await _ctx.Set<Distribuidor>()
                         .AsNoTracking()
                         .OrderBy(d => d.Nombre)
                         .ToListAsync();

        public async Task<Distribuidor?> GetByIdAsync(int id)
            => await _ctx.Set<Distribuidor>()
                         .AsNoTracking()
                         .FirstOrDefaultAsync(d => d.DistribuidorId == id);

        public async Task<Distribuidor?> GetByGmailAsync(string gmail)
            => await _ctx.Set<Distribuidor>()
                         .AsNoTracking()
                         .FirstOrDefaultAsync(d => d.Gmail == gmail);

        public async Task<Distribuidor> AddAsync(Distribuidor entity)
        {
            _ctx.Set<Distribuidor>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Distribuidor entity)
        {
            _ctx.Set<Distribuidor>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _ctx.Set<Distribuidor>().FindAsync(id);
            if (current is null) return false;
            _ctx.Set<Distribuidor>().Remove(current);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public Task<bool> ExistsAsync(int id)
            => _ctx.Set<Distribuidor>().AnyAsync(d => d.DistribuidorId == id);

        public Task<bool> GmailInUseAsync(string gmail, int? excludeId = null)
            => _ctx.Set<Distribuidor>()
                   .AnyAsync(d => d.Gmail == gmail && (excludeId == null || d.DistribuidorId != excludeId));
    }
}
