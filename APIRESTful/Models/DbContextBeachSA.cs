using Microsoft.EntityFrameworkCore;

namespace APIRESTful.Models
{
    public class DbContextBeachSA : DbContext
    {
        public DbContextBeachSA(DbContextOptions<DbContextBeachSA> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Paquete> Paquetes { get; set; }
        public DbSet<TipoPago> TiposPago { get; set; }
        public DbSet<Reservacion> Reservaciones { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }

        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);

            // ===== app.Clientes =====
            model.Entity<Cliente>(e =>
            {
                e.ToTable("Clientes", "app");
                e.HasKey(x => x.cedula);
                e.Property(x => x.cedula).HasMaxLength(12).IsRequired();
                e.Property(x => x.tipo_cedula).HasMaxLength(20).IsRequired();
                e.Property(x => x.fullname).HasMaxLength(100).IsRequired();
                e.Property(x => x.telefono).HasMaxLength(20).IsRequired();
                e.Property(x => x.direccion).HasMaxLength(200);
                e.Property(x => x.email).HasMaxLength(254).IsRequired();
            });

            // ===== app.Paquetes =====
            model.Entity<Paquete>(e =>
            {
                e.ToTable("Paquetes", "app");
                e.HasKey(x => x.paquete_id);
                e.Property(x => x.nombre).HasMaxLength(30).IsRequired();
                e.Property(x => x.costo_por_persona_noche)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();
                e.Property(x => x.prima_porcentaje)
                    .HasColumnType("decimal(5,2)")
                    .IsRequired();
                e.Property(x => x.mensualidades).IsRequired();
            });

            // ===== app.TipoPago =====
            model.Entity<TipoPago>(e =>
            {
                e.ToTable("TipoPago", "app");
                e.HasKey(x => x.tipo_pago_id);
                e.Property(x => x.nombre).HasMaxLength(20).IsRequired();
            });

            // ===== app.Reservaciones =====
            model.Entity<Reservacion>(e =>
            {
                e.ToTable("Reservaciones", "app");
                e.HasKey(x => x.reservacion_id);

                // FK: cliente_cedula -> app.Clientes.cedula
                e.HasOne(x => x.cliente)
                 .WithMany()
                 .HasForeignKey(x => x.cliente_cedula)
                 .HasConstraintName("FK_Resv_Cliente");

                // FK: paquete_id -> app.Paquetes.paquete_id
                e.HasOne(x => x.paquete)
                 .WithMany()
                 .HasForeignKey(x => x.paquete_id)
                 .HasConstraintName("FK_Resv_Paquete");

                // FK: metodo_pago_id -> app.TipoPago.tipo_pago_id
                e.HasOne(x => x.tipo_pago)
                 .WithMany()
                 .HasForeignKey(x => x.metodo_pago_id)
                 .HasConstraintName("FK_Resv_TipoPago");

                // Tipos numéricos iguales a la BD
                e.Property(x => x.descuento_pct).HasColumnType("decimal(5,2)");
                e.Property(x => x.subtotal_colones).HasColumnType("decimal(12,2)");
                e.Property(x => x.descuento_colones).HasColumnType("decimal(12,2)");
                e.Property(x => x.iva_pct).HasColumnType("decimal(5,2)");
                e.Property(x => x.iva_colones).HasColumnType("decimal(12,2)");
                e.Property(x => x.total_colones).HasColumnType("decimal(12,2)");
                e.Property(x => x.prima_porcentaje).HasColumnType("decimal(5,2)");
                e.Property(x => x.prima_monto).HasColumnType("decimal(12,2)");
                e.Property(x => x.mensualidades).IsRequired();

                // Campos calculados que NO existen en la tabla
                e.Ignore(x => x.tipo_cambio_usd);
                e.Ignore(x => x.total_usd);
            });

            // ===== app.Facturas (USD congelado) =====
            model.Entity<Factura>(e =>
            {
                e.ToTable("Facturas", "app");
                e.HasKey(x => x.factura_id);

                e.HasOne(x => x.reservacion)
                 .WithOne()
                 .HasForeignKey<Factura>(x => x.reservacion_id)
                 .HasConstraintName("FK_Fact_Resv");

                e.Property(x => x.tipo_cambio_usd)
                    .HasColumnType("decimal(12,4)")
                    .IsRequired();

                e.Property(x => x.total_usd)
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();
            });

            

        }
    }
}
