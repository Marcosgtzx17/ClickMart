using ClickMart.DTOs.CodigoConfirmacionDTOs;


namespace ClickMart.Interfaces
{
    public interface ICodigoConfirmacionService
    {
        Task<CodigoConfirmacionResponseDTO> GenerarAsync(string email);
        Task<bool> ValidarAsync(string email, string codigo);
    }
}