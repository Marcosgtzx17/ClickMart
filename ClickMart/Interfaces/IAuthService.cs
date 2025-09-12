
using ClickMart.DTOs.UsuariosDTOs;

namespace ClickMart.Interfaces
{
    public interface IAuthService
    {
        Task<UsuarioRespuestaDTO?> RegistrarAsync(UsuarioRegistroDTO dto);
        Task<UsuarioRespuestaDTO?> LoginAsync(UsuarioLoginDTO dto);
    }
}
