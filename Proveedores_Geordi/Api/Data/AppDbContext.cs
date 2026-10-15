using ComprasApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ComprasApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> ComprasDetalle => Set<CompraDetalle>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Proveedor>().HasIndex(p => p.Nit).IsUnique();

        mb.Entity<Compra>()
          .HasOne(c => c.Proveedor).WithMany()
          .HasForeignKey(c => c.IdProveedor).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<CompraDetalle>()
          .HasOne(d => d.Compra).WithMany(c => c.Detalles)
          .HasForeignKey(d => d.IdCompra).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CompraDetalle>()
          .HasOne(d => d.Producto).WithMany()
          .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
    }
}
