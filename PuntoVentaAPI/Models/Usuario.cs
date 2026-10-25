using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdEmpleado { get; set; }

    public string Usuario1 { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public bool? Estado { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;
}
