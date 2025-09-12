using ClickMart.Entidades;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ClickMart.Repositorios
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<CategoriaProducto> CategoriasProducto { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Email único en usuarios
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 1 Rol -> N Usuarios
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId);

            // Mapeo explícito por si hace falta ajustar algo adicional
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(e => e.UsuarioId);
            });

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.RolId);
            });

            modelBuilder.Entity<CategoriaProducto>(entity =>
            {
                entity.ToTable("categoria_productos");
                entity.HasKey(e => e.CategoriaId);
                entity.Property(e => e.Nombre).HasMaxLength(120).IsRequired();
            });
        }
    }
}
