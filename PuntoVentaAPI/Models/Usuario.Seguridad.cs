
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoVentaAPI.Models;

public partial class Usuario
{
    public string Rol { get; set; } = "VENDEDOR";

    [Column("debe_cambiar_contrasena")]
    public bool DebeCambiarContrasena { get; set; }
}