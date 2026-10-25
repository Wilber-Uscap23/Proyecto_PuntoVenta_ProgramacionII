using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class Compra
{
    public int IdCompra { get; set; }

    public int IdProveedor { get; set; }

    public DateTime FechaCompra { get; set; }

    public decimal Total { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<ComprasDetalle> ComprasDetalles { get; set; } = new List<ComprasDetalle>();

    public virtual Proveedore IdProveedorNavigation { get; set; } = null!;
}
