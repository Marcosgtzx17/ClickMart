using System.Linq;
using ClickMart.DTOs.PedidoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepository _repo;
        private readonly IDetallePedidoRepository _detRepo;

        public PedidoService(IPedidoRepository repo, IDetallePedidoRepository detRepo)
        {
            _repo = repo;
            _detRepo = detRepo;
        }

        public async Task<List<PedidoResponseDTO>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(ToDto).ToList();

        public async Task<PedidoResponseDTO?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e is null ? null : ToDto(e);
        }

        public async Task<PedidoResponseDTO> CreateAsync(PedidoCreateDTO dto)
        {
            var entity = new Pedido
            {
                UsuarioId = dto.UsuarioId,
                Total = 0m, // ← inicia en 0; se recalcula con DetallePedido
                Fecha = dto.Fecha,
                MetodoPago = dto.MetodoPago,
                PagoEstado = dto.PagoEstado,
                TarjetaUltimos4 = null
            };

            if (dto.MetodoPago == MetodoPago.TARJETA)
            {
                if (string.IsNullOrWhiteSpace(dto.NumeroTarjeta))
                    throw new ArgumentException("Se requiere el número de tarjeta.");

                if (!LuhnValidator.IsValid(dto.NumeroTarjeta))
                    throw new ArgumentException("Tarjeta inválida (Luhn).");

                entity.TarjetaUltimos4 = LuhnValidator.Last4(dto.NumeroTarjeta);
            }

            var saved = await _repo.AddAsync(entity);
            return ToDto(saved);
        }

        public async Task<bool> UpdateAsync(int id, PedidoUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            current.Fecha = dto.Fecha;
            current.MetodoPago = dto.MetodoPago;
            current.PagoEstado = dto.PagoEstado;
            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        public async Task<bool> RecalcularTotalAsync(int pedidoId)
        {
            var pedido = await _repo.GetByIdAsync(pedidoId);
            if (pedido is null) return false;
            var detalles = await _detRepo.GetByPedidoAsync(pedidoId);
            pedido.Total = detalles.Sum(d => d.Subtotal);
            return await _repo.UpdateAsync(pedido);
        }

        public async Task<bool> MarcarPagadoAsync(int pedidoId)
        {
            var pedido = await _repo.GetByIdAsync(pedidoId);
            if (pedido is null) return false;
            pedido.PagoEstado = EstadoPago.PAGADO;
            return await _repo.UpdateAsync(pedido);
        }

        private static PedidoResponseDTO ToDto(Pedido e) => new()
        {
            PedidoId = e.PedidoId,
            UsuarioId = e.UsuarioId,
            Total = e.Total,
            Fecha = e.Fecha,
            MetodoPago = e.MetodoPago,
            PagoEstado = e.PagoEstado,
            TarjetaUltimos4 = e.TarjetaUltimos4
        };
    }
}
