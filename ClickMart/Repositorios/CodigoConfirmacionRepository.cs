using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ClickMart.Repositorios
{
    public class CodigoConfirmacionRepository : ICodigoConfirmacionRepository
    {
        private readonly AppDbContext _ctx;
        public CodigoConfirmacionRepository(AppDbContext ctx) => _ctx = ctx;


        public async Task<CodigoConfirmacion> AddAsync(CodigoConfirmacion entity)
        {
            _ctx.Set<CodigoConfirmacion>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }


        public async Task<CodigoConfirmacion?> GetUsableAsync(string email, string codigo, DateTime minFecha)
        {
            return await _ctx.Set<CodigoConfirmacion>()
            .Where(c => c.Email == email && c.Codigo == codigo && c.Usado == 0 && c.FechaGeneracion >= minFecha)
            .OrderByDescending(c => c.FechaGeneracion)
            .FirstOrDefaultAsync();
        }


        public async Task<bool> MarkUsedAsync(int idCodigo)
        {
            var entity = await _ctx.Set<CodigoConfirmacion>().FindAsync(idCodigo);
            if (entity is null) return false;
            entity.Usado = 1;
            _ctx.Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}