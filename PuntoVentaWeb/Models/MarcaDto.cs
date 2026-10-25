
namespace PuntoVentaWeb.Models;

public class MarcaDto
{
    public int IdMarca { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool? Estado { get; set; }
}
