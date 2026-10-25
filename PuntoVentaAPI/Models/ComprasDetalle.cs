using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class ComprasDetalle
{
    public int IdDetalleCompra { get; set; }

    public int IdCompra { get; set; }

    public int IdProducto { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioCosto { get; set; }

    public decimal Subtotal { get; set; }

    public virtual Compra IdCompraNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
