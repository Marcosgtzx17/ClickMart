// Repositorios/RolRepository.cs
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class RolRepository : IRolRepository
    {
        private readonly AppDbContext _ctx;
        public RolRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<Rol>> GetAllAsync()
            => await _ctx.Roles.AsNoTracking().ToListAsync();

        public Task<Rol?> GetByIdAsync(int id)
            => _ctx.Roles.FindAsync(id).AsTask();

        public async Task<Rol?> GetByNameAsync(string nombre)
            => await _ctx.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nombre == nombre);

        public async Task<Rol> AddAsync(Rol entity)
        {
            _ctx.Roles.Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Rol entity)
        {
            _ctx.Roles.Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Roles.FindAsync(id);
            if (existing is null) return false;
            _ctx.Roles.Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}