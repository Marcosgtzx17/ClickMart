using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class DistribuidorRepository : IDistribuidorRepository
    {
        private readonly AppDbContext _ctx;

        public DistribuidorRepository(AppDbContext ctx) => _ctx = ctx;

        // ===== Validación + normalización =====
        private static void Validar(Distribuidor e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            if (string.IsNullOrWhiteSpace(e.Nombre) ||
                string.IsNullOrWhiteSpace(e.Telefono) ||
                string.IsNullOrWhiteSpace(e.Direccion) ||
                string.IsNullOrWhiteSpace(e.Gmail))
            {
                throw new ArgumentException("Complete todos los campos obligatorios");
            }

            e.Nombre = e.Nombre.Trim();
            e.Telefono = e.Telefono.Trim();
            e.Direccion = e.Direccion.Trim();
            e.Gmail = e.Gmail.Trim();
        }

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
        {
            var mail = gmail?.Trim();
            return await _ctx.Set<Distribuidor>()
                             .AsNoTracking()
                             .FirstOrDefaultAsync(d => d.Gmail == mail);
        }

        public async Task<Distribuidor> AddAsync(Distribuidor entity)
        {
            Validar(entity);
            _ctx.Set<Distribuidor>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Distribuidor entity)
        {
            Validar(entity);

            // Evita conflicto de tracking: carga la existente y copia valores
            var existing = await _ctx.Set<Distribuidor>().FindAsync(entity.DistribuidorId);
            if (existing is null) return false;

            _ctx.Entry(existing).CurrentValues.SetValues(entity);
            _ctx.Entry(existing).State = EntityState.Modified;

            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _ctx.Set<Distribuidor>().FindAsync(id);
            if (current is null) return false;

            // Bloqueo por referencias activas (productos asociados)
            // ⚠ Ajusta "DistribuidorId" si tu FK tiene otro nombre.
            bool enUso = await _ctx.Set<Productos>().AnyAsync(p => p.DistribuidorId == id);
            if (enUso) return false;

            _ctx.Set<Distribuidor>().Remove(current);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public Task<bool> ExistsAsync(int id)
            => _ctx.Set<Distribuidor>().AnyAsync(d => d.DistribuidorId == id);

        public Task<bool> GmailInUseAsync(string gmail, int? excludeId = null)
        {
            var mail = gmail?.Trim();
            return _ctx.Set<Distribuidor>()
                       .AnyAsync(d => d.Gmail == mail && (excludeId == null || d.DistribuidorId != excludeId));
        }
    }
}