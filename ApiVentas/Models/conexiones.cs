using Microsoft.EntityFrameworkCore;
namespace ApiVentas.Models;
public class Conexiones:DbContext
{
    public Conexiones(DbContextOptions<Conexiones> options):base(options)
    {
        
    }
    public DbSet<Venta> ventas{get; set;}
}