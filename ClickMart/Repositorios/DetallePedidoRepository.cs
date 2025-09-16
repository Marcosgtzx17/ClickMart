using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ClickMart.Repositorios
{
    public class DetallePedidoRepository : IDetallePedidoRepository
    {
        private readonly AppDbContext _ctx;
        public DetallePedidoRepository(AppDbContext ctx) => _ctx = ctx;


        public async Task<List<DetallePedido>> GetAllAsync() =>
        await _ctx.Set<DetallePedido>().AsNoTracking().ToListAsync();


        public async Task<List<DetallePedido>> GetByPedidoAsync(int pedidoId) =>
        await _ctx.Set<DetallePedido>().AsNoTracking()
        .Where(d => d.IdPedido == pedidoId)
        .ToListAsync();


        public async Task<DetallePedido?> GetByIdAsync(int id) =>
        await _ctx.Set<DetallePedido>().FindAsync(id);


        public async Task<DetallePedido> AddAsync(DetallePedido entity)
        {
            _ctx.Set<DetallePedido>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }


        public async Task<bool> UpdateAsync(DetallePedido entity)
        {
            _ctx.Set<DetallePedido>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<DetallePedido>().FindAsync(id);
            if (existing is null) return false;
            _ctx.Set<DetallePedido>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}