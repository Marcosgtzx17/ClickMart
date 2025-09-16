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
        public DbSet<Distribuidor> Distribuidores { get; set; } = null!;
        public DbSet<Productos> Productos { get; set; } = null!;
        public DbSet<Resena> Reseñas { get; set; } = null!; // tabla con ñ

        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<DetallePedido> DetallePedidos { get; set; } = null!;
        public DbSet<CodigoConfirmacion> CodigosConfirmacion { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Distribuidor>(entity =>
            {
                entity.ToTable("distribuidores");
                entity.HasKey(e => e.DistribuidorId);

                // Opcional: longitudes
                entity.Property(e => e.Nombre).HasMaxLength(120);
                entity.Property(e => e.Direccion).HasMaxLength(200);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Gmail).HasMaxLength(120);
                entity.Property(e => e.Descripcion).HasMaxLength(250);

                // Índice único sugerido para Gmail (si tu DB lo permite)
                entity.HasIndex(e => e.Gmail).IsUnique();
            });

            modelBuilder.Entity<Productos>(entity =>
            {
                entity.ToTable("productos");
                entity.HasKey(e => e.ProductoId);

                entity.HasOne(p => p.Distribuidor)
                      .WithMany(d => d.Productos)
                      .HasForeignKey(p => p.DistribuidorId)
                      .OnDelete(DeleteBehavior.SetNull); // evita borrado en cascada no deseado
            });

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
            modelBuilder.Entity<Resena>(entity =>
            {
                entity.ToTable("reseñas");                       // tabla con ñ
                entity.HasKey(r => r.ResenaId);
                entity.Property(r => r.ResenaId).HasColumnName("RESEÑA_ID");
                entity.Property(r => r.UsuarioId).HasColumnName("USUARIO_ID").IsRequired();
                entity.Property(r => r.ProductoId).HasColumnName("PRODUCTO_ID").IsRequired();
                entity.Property(r => r.Calificacion).HasColumnName("calificacion").IsRequired();
                entity.Property(r => r.Comentario).HasColumnName("comentario").HasColumnType("TEXT");
                entity.Property(r => r.FechaResena).HasColumnName("fecha_reseña").IsRequired();

                entity.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId);
                entity.HasOne(r => r.Producto).WithMany().HasForeignKey(r => r.ProductoId);
            });

            // Pedido
            // Pedido
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.ToTable("pedidos");
                entity.HasKey(p => p.PedidoId);

                // === Conversión enum <-> string (FIX del 500) ===
                entity.Property(p => p.MetodoPago)
                      .HasConversion<string>()            // guarda/lee strings (EFECTIVO/TARJETA)
                      .HasMaxLength(20)
                      .HasColumnName("METODO_PAGO");

                entity.Property(p => p.PagoEstado)
                      .HasConversion<string>()            // guarda/lee strings (PENDIENTE/PAGADO)
                      .HasMaxLength(20)
                      .HasColumnName("PAGO_ESTADO");

                // (Opcional) explícita nombres/tipos si quieres alinear con tu DDL
                entity.Property(p => p.PedidoId).HasColumnName("PEDIDO_ID");
                entity.Property(p => p.UsuarioId).HasColumnName("ID_USUARIO");
                entity.Property(p => p.Total).HasColumnName("TOTAL").HasColumnType("decimal(10,2)");
                entity.Property(p => p.Fecha).HasColumnName("FECHA");
                entity.Property(p => p.TarjetaUltimos4).HasColumnName("TARJETA_ULTIMOS4").HasMaxLength(4);

                entity.HasOne(p => p.Usuario)
                      .WithMany()
                      .HasForeignKey(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });



            // DetallePedido
            modelBuilder.Entity<DetallePedido>(entity =>
            {
                entity.ToTable("detalle_pedidos");
                entity.HasKey(d => d.DetalleId);


                entity.HasOne(d => d.Pedido)
                .WithMany() // si prefieres, agrega ICollection<DetallePedido> en Pedido
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.Cascade);


                entity.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<CodigoConfirmacion>(entity =>
            {
                entity.ToTable("CodigoConfirmacion");
                entity.HasKey(c => c.IdCodigo);

                entity.Property(c => c.IdCodigo).HasColumnName("id_codigo");
                entity.Property(c => c.Email).HasColumnName("Email").HasMaxLength(100).IsRequired();
                entity.Property(c => c.Codigo).HasColumnName("Codigo").HasMaxLength(50).IsRequired();

                entity.Property(c => c.FechaGeneracion)
                      .HasColumnName("fecha_generacion")   // <-- ¡AQUÍ también!
                      .IsRequired();

                entity.Property(c => c.Usado)
                      .HasColumnName("Usado")
                      .HasDefaultValue(0);

                entity.HasIndex(c => c.Email);
            });
        }
    }
}
