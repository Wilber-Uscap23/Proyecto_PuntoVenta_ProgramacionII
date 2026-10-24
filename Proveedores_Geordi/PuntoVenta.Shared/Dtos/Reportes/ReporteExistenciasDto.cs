// Módulo: Reportes — Rodrigo Rodríguez
namespace PuntoVenta.Shared.Dtos.Reportes;

public static class EstadoStock
{
    public const string Agotado = "AGOTADO";
    public const string Bajo = "BAJO";
    public const string Disponible = "DISPONIBLE";
}

public class ReporteExistenciaFilaDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public int Existencia { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    /// <summary>Existencia * precio de costo.</summary>
    public decimal ValorCosto { get; set; }
    /// <summary>Existencia * precio de venta.</summary>
    public decimal ValorVenta { get; set; }
    /// <summary>AGOTADO, BAJO o DISPONIBLE (ver <see cref="EstadoStock"/>).</summary>
    public string EstadoStock { get; set; } = string.Empty;
}

public class ReporteExistenciasDto
{
    public DateTime FechaGeneracion { get; set; }
    public int StockMinimo { get; set; }
    public List<ReporteExistenciaFilaDto> Productos { get; set; } = new();
    public int TotalProductos { get; set; }
    public int TotalUnidades { get; set; }
    public decimal ValorTotalCosto { get; set; }
    public decimal ValorTotalVenta { get; set; }
    public int ProductosAgotados { get; set; }
    public int ProductosBajos { get; set; }
}
