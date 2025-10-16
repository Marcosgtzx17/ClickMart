using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class RolRepository : IRolRepository
    {
        private readonly AppDbContext _ctx;
        public RolRepository(AppDbContext ctx) => _ctx = ctx;

        // Validación + normalización
        private static void Validar(Rol entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrWhiteSpace(entity.Nombre))
                throw new ArgumentException("El nombre del rol es obligatorio");
            entity.Nombre = entity.Nombre.Trim();
        }

        public async Task<List<Rol>> GetAllAsync()
            => await _ctx.Roles.AsNoTracking().ToListAsync();

        public Task<Rol?> GetByIdAsync(int id)
            => _ctx.Roles.FindAsync(id).AsTask();

        public async Task<Rol?> GetByNameAsync(string nombre)
        {
            var n = nombre?.Trim();
            return await _ctx.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nombre == n);
        }

        public async Task<Rol> AddAsync(Rol entity)
        {
            Validar(entity);
            _ctx.Roles.Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Rol entity)
        {
            Validar(entity);

            // Evita conflicto de tracking: carga la existente y copia valores
            var existing = await _ctx.Roles.FindAsync(entity.RolId);
            if (existing is null) return false;

            _ctx.Entry(existing).CurrentValues.SetValues(entity);
            _ctx.Entry(existing).State = EntityState.Modified;

            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Roles.FindAsync(id);
            if (existing is null) return false;

            // HU-1028: no permitir si hay usuarios con este rol
            // Ajusta "RolId" si tu entidad Usuario usa otro nombre.
            bool enUso = await _ctx.Set<Usuario>().AnyAsync(u => u.RolId == id);
            if (enUso) return false;

            _ctx.Roles.Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}