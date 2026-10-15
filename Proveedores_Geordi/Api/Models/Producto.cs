using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComprasApp.Api.Models;

// STUB: entidad compartida con Wilber (módulo Productos/Marcas).
// Reemplazar por la entidad real manteniendo los nombres de columnas
// existencia, precio_costo y precio_venta.
[Table("Productos")]
public class Producto
{
    [Key, Column("id_producto")] public int IdProducto { get; set; }
    [Required, StringLength(150), Column("descripcion")] public string Descripcion { get; set; } = "";
    [Column("id_marca")] public int? IdMarca { get; set; }
    [Column("precio_costo", TypeName = "decimal(18,2)")] public decimal PrecioCosto { get; set; }
    [Column("precio_venta", TypeName = "decimal(18,2)")] public decimal PrecioVenta { get; set; }
    [Column("existencia")] public int Existencia { get; set; }
    [Column("fecha_ingreso")] public DateTime FechaIngreso { get; set; } = DateTime.Now;
    [StringLength(255), Column("imagen")] public string? Imagen { get; set; }
}
