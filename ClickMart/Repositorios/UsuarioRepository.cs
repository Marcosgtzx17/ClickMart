using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ClickMart.Repositorios
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;

        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        // ===== Validaciones / Normalización =====
        private static void Validar(Usuario u)
        {
            if (u is null) throw new ArgumentNullException(nameof(u));

            if (string.IsNullOrWhiteSpace(u.Nombre) ||
                string.IsNullOrWhiteSpace(u.Email) ||
                string.IsNullOrWhiteSpace(u.Telefono) ||
                string.IsNullOrWhiteSpace(u.Direccion))
            {
                throw new ArgumentException("Complete todos los campos obligatorios");
            }

            // Email válido
            var emailAttr = new EmailAddressAttribute();
            if (!emailAttr.IsValid(u.Email?.Trim()))
                throw new ArgumentException("Formato de correo inválido");

            // Normalización
            u.Nombre = u.Nombre.Trim();
            u.Email = u.Email.Trim();
            u.Telefono = u.Telefono.Trim();
            u.Direccion = u.Direccion.Trim();
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            var mail = email?.Trim();
            return await _context.Usuarios
                                 .Include(u => u.Rol)
                                 .FirstOrDefaultAsync(u => u.Email == mail);
        }

        public async Task<Usuario> AddAsync(Usuario usuario)
        {
            Validar(usuario);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<List<UsuarioListadoDTO>> GetAllUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                                         .Include(u => u.Rol)
                                         .AsNoTracking()
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
            => await _context.Usuarios
                             .Include(u => u.Rol)
                             .AsNoTracking()
                             .FirstOrDefaultAsync(u => u.UsuarioId == id);

        public async Task<bool> UpdateAsync(Usuario usuario)
        {
            Validar(usuario);

            // Evita conflicto de tracking: carga la existente y copia valores
            var existing = await _context.Usuarios.FindAsync(usuario.UsuarioId);
            if (existing is null) return false;

            _context.Entry(existing).CurrentValues.SetValues(usuario);
            _context.Entry(existing).State = EntityState.Modified;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Usuarios.FindAsync(id);
            if (existing is null) return false;

            // 🔒 Restricción por dependencias críticas (ajusta tipos/FKs si difieren)
            bool tienePedidos = await _context.Set<Pedido>().AnyAsync(p => p.UsuarioId == id);
            bool tieneResenas = await _context.Set<Resena>().AnyAsync(r => r.UsuarioId == id);

            if (tienePedidos || tieneResenas) return false;

            _context.Usuarios.Remove(existing);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}