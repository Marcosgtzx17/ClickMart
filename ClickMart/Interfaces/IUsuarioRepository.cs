
using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;

namespace ClickMart.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario> AddAsync(Usuario usuario);
        Task<List<UsuarioListadoDTO>> GetAllUsuariosAsync();
    }
}
