
namespace PuntoVentaWeb.Models;

public class ProductoDto
{
    public int IdProducto { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public decimal PrecioCosto { get; set; }

    public decimal PrecioVenta { get; set; }

    public int Existencia { get; set; }

    public int IdMarca { get; set; }

    public DateTime FechaIngreso { get; set; }

    public string? ImagenUrl { get; set; }

    public bool? Estado { get; set; }
}
