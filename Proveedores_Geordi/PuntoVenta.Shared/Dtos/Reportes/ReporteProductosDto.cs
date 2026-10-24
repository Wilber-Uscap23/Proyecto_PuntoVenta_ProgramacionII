// Módulo: Reportes — Rodrigo Rodríguez
namespace PuntoVenta.Shared.Dtos.Reportes;

public class ReporteProductoFilaDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    /// <summary>Precio de venta - precio de costo.</summary>
    public decimal Ganancia { get; set; }
    public int Existencia { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; }
}

public class ReporteProductosDto
{
    public DateTime FechaGeneracion { get; set; }
    public List<ReporteProductoFilaDto> Productos { get; set; } = new();
    public int TotalProductos { get; set; }
    public int TotalActivos { get; set; }
    public int TotalInactivos { get; set; }
}
