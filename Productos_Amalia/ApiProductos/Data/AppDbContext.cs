using ApiProductos.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiProductos.Data;

/// <summary>
/// Contexto del módulo de Amalia (Marcas y Productos). Las tablas ya existen en
/// database/PuntoVentaDB.sql, por eso NO se usan migraciones aquí: cualquier cambio de
/// estructura se coordina con Wilber (responsable de la base de datos).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Marca)
            .WithMany(m => m.Productos)
            .HasForeignKey(p => p.IdMarca)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
