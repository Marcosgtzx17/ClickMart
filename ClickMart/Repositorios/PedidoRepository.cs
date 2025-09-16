using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ClickMart.Repositorios
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly AppDbContext _ctx;
        public PedidoRepository(AppDbContext ctx) => _ctx = ctx;


        public async Task<List<Pedido>> GetAllAsync() =>
        await _ctx.Set<Pedido>().AsNoTracking().ToListAsync();


        public async Task<Pedido?> GetByIdAsync(int id) =>
        await _ctx.Set<Pedido>().FindAsync(id);


        public async Task<Pedido> AddAsync(Pedido entity)
        {
            _ctx.Set<Pedido>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }


        public async Task<bool> UpdateAsync(Pedido entity)
        {
            _ctx.Set<Pedido>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<Pedido>().FindAsync(id);
            if (existing is null) return false;
            _ctx.Set<Pedido>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}