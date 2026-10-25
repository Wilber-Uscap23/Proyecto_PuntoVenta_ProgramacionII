using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class Proveedore
{
    public int IdProveedor { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Nit { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Direccion { get; set; }

    public string? Contacto { get; set; }

    public DateTime FechaRegistro { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
