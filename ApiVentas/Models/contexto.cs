using Microsoft.EntityFrameworkCore;
using ApiVentas.Models;

namespace ApiVentas.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Módulo de ventas
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentasDetalle> VentasDetalle { get; set; }

        // Módulo de clientes
        public DbSet<Cliente> Clientes { get; set; }

        // Facturas (número FEL autoincrementable)
        public DbSet<Factura> Facturas { get; set; }

        // Referencias de solo lectura (empleados y productos)
        public DbSet<EmpleadoRef> EmpleadosRef { get; set; }
        public DbSet<ProductoRef> ProductosRef { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Une las dos tablas: ventas (1) -> ventas_detalle (muchos)
            modelBuilder.Entity<VentasDetalle>()
                .HasOne(d => d.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(d => d.id_venta);
        }
    }
}