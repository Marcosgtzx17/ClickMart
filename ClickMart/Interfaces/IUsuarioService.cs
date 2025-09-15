
using ClickMart.DTOs.UsuariosDTOs;

namespace ClickMart.Interfaces
{
    public interface IUsuarioService
    {
        Task<UsuarioListadoDTO?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<UsuarioListadoDTO> CreateAsync(UsuarioCreateDTO dto);
    }
}
