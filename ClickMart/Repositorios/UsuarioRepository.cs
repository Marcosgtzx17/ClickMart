
using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;

        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _context.Usuarios
                                 .Include(u => u.Rol)
                                 .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario> AddAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<List<UsuarioListadoDTO>> GetAllUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                                         .Include(u => u.Rol)
                                         .ToListAsync();

            return usuarios.Select(u => new UsuarioListadoDTO
            {
                UsuarioId = u.UsuarioId,
                Nombre = u.Nombre,
                Email = u.Email,
                Telefono = u.Telefono,
                Direccion = u.Direccion,
                Rol = u.Rol?.Nombre ?? "Usuario",
            }).ToList();
        }
        public async Task<Usuario?> GetByIdAsync(int id)
         => await _context.Usuarios.Include(u => u.Rol)
                                   .FirstOrDefaultAsync(u => u.UsuarioId == id);

        public async Task<bool> UpdateAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Usuarios.FindAsync(id);
            if (existing is null) return false;
            _context.Usuarios.Remove(existing);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
