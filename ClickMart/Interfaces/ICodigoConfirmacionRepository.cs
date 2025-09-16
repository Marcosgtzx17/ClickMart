using ClickMart.Entidades;


namespace ClickMart.Interfaces
{
    public interface ICodigoConfirmacionRepository
    {
        Task<CodigoConfirmacion> AddAsync(CodigoConfirmacion entity);
        Task<CodigoConfirmacion?> GetUsableAsync(string email, string codigo, DateTime minFecha);
        Task<bool> MarkUsedAsync(int idCodigo);
    }
}