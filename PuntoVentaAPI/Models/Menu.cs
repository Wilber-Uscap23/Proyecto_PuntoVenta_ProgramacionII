using System;
using System.Collections.Generic;

namespace PuntoVentaAPI.Models;

public partial class Menu
{
    public int IdMenu { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Ruta { get; set; }

    public string? Icono { get; set; }

    public int? IdMenuPadre { get; set; }

    public int Orden { get; set; }

    public bool? Estado { get; set; }

    public virtual Menu? IdMenuPadreNavigation { get; set; }

    public virtual ICollection<Menu> InverseIdMenuPadreNavigation { get; set; } = new List<Menu>();
}
