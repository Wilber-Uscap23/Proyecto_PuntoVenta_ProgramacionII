// =====================================================================
//  BASE DE INTEGRACIÓN — el DbContext real es responsabilidad de Wilber.
//
//  Lo ÚNICO que el módulo de Rodrigo necesita en el DbContext de Wilber es:
//
//      public DbSet<Menu> Menus => Set<Menu>();
//
//  (más los DbSet de Productos, Marcas, Compras, Ventas, etc. que ya
//   agregan sus compañeros y que los reportes solo LEEN).
// =====================================================================
using Microsoft.EntityFrameworkCore;
using PuntoVenta.API.Models;

namespace PuntoVenta.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // --- Módulo de Rodrigo ---
    public DbSet<Menu> Menus => Set<Menu>();

    // --- Módulos de los compañeros (solo lectura para los reportes) ---
    public DbSet<Puesto> Puestos => Set<Puesto>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> ComprasDetalle => Set<CompraDetalle>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentasDetalle => Set<VentaDetalle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Menú: igual que en MySQL (ON DELETE RESTRICT), no se puede borrar
        // una opción padre si todavía tiene hijos.
        modelBuilder.Entity<Menu>()
            .HasOne(m => m.MenuPadre)
            .WithMany(m => m.Hijos)
            .HasForeignKey(m => m.IdMenuPadre)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Usuario>().HasIndex(u => u.IdEmpleado).IsUnique();
    }
}
