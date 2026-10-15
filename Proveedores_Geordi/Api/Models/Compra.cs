using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComprasApp.Api.Models;

[Table("Compras")]
public class Compra
{
    [Key, Column("id_compra")] public int IdCompra { get; set; }
    [Column("fecha")] public DateTime Fecha { get; set; } = DateTime.Now;
    [Column("id_proveedor")] public int IdProveedor { get; set; }
    [Column("total", TypeName = "decimal(18,2)")] public decimal Total { get; set; }

    public Proveedor? Proveedor { get; set; }
    public List<CompraDetalle> Detalles { get; set; } = new();
}

[Table("Compras_Detalle")]
public class CompraDetalle
{
    [Key, Column("id_compra_detalle")] public int IdCompraDetalle { get; set; }
    [Column("id_compra")] public int IdCompra { get; set; }
    [Column("id_producto")] public int IdProducto { get; set; }
    [Column("cantidad")] public int Cantidad { get; set; }
    [Column("precio_costo", TypeName = "decimal(18,2)")] public decimal PrecioCosto { get; set; }
    [Column("subtotal", TypeName = "decimal(18,2)")] public decimal Subtotal { get; set; }

    public Compra? Compra { get; set; }
    public Producto? Producto { get; set; }
}
