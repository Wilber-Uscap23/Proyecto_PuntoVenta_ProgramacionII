using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProductos.Models;

[Table("productos")]
public class Producto
{
    [Key, Column("id_producto")] public int IdProducto { get; set; }
    [Required, MaxLength(50), Column("codigo")] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(255), Column("descripcion")] public string Descripcion { get; set; } = string.Empty;
    [Column("id_marca")] public int IdMarca { get; set; }
    [Column("precio_costo", TypeName = "decimal(12,2)")] public decimal PrecioCosto { get; set; }
    [Column("precio_venta", TypeName = "decimal(12,2)")] public decimal PrecioVenta { get; set; }
    [Column("existencia")] public int Existencia { get; set; }
    [Column("fecha_ingreso")] public DateTime FechaIngreso { get; set; }
    [MaxLength(500), Column("imagen_url")] public string? ImagenUrl { get; set; }
    [Column("estado")] public bool Estado { get; set; } = true;

    [ForeignKey(nameof(IdMarca))] public Marca? Marca { get; set; }
}
