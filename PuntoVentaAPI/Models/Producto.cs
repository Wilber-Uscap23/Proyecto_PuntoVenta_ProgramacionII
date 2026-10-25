using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int IdMarca { get; set; }

    public decimal PrecioCosto { get; set; }

    public decimal PrecioVenta { get; set; }

    public int Existencia { get; set; }

    public DateTime FechaIngreso { get; set; }

    public string? ImagenUrl { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<ComprasDetalle> ComprasDetalles { get; set; } = new List<ComprasDetalle>();

    public virtual Marca IdMarcaNavigation { get; set; } = null!;

    public virtual ICollection<VentasDetalle> VentasDetalles { get; set; } = new List<VentasDetalle>();
}
