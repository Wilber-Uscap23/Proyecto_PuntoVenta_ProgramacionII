using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Direccion { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public int IdPuesto { get; set; }

    public bool? Estado { get; set; }

    public virtual Puesto IdPuestoNavigation { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
