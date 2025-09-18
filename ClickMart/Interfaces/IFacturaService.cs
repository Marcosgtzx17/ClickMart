using System.Threading.Tasks;

namespace ClickMart.Interfaces
{
    public interface IFacturaService
    {
        Task<byte[]?> GenerarFacturaPdfAsync(int pedidoId);
    }
}
