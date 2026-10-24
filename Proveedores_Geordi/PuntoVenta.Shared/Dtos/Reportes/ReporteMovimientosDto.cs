// Módulo: Reportes — Rodrigo Rodríguez
namespace PuntoVenta.Shared.Dtos.Reportes;

public static class EstadoMovimiento
{
    public const string Registrada = "REGISTRADA";
    public const string Anulada = "ANULADA";
}

/// <summary>Una línea del detalle de una compra o una venta.</summary>
public class ReporteDetalleLineaDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

// ----------------------------- COMPRAS -----------------------------

public class ReporteCompraFilaDto
{
    public int IdCompra { get; set; }
    public DateTime Fecha { get; set; }
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? NitProveedor { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int CantidadArticulos { get; set; }
    public List<ReporteDetalleLineaDto> Detalle { get; set; } = new();
}

public class ReporteComprasDto
{
    public DateTime FechaGeneracion { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public List<ReporteCompraFilaDto> Compras { get; set; } = new();
    /// <summary>Compras con estado REGISTRADA.</summary>
    public int CantidadCompras { get; set; }
    public int CantidadAnuladas { get; set; }
    /// <summary>Suma del total de las compras REGISTRADAS (las anuladas no suman).</summary>
    public decimal TotalComprado { get; set; }
}

// ------------------------------ VENTAS ------------------------------

public class ReporteVentaFilaDto
{
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int IdCliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string? NitCliente { get; set; }
    public int IdEmpleado { get; set; }
    public string Empleado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int CantidadArticulos { get; set; }
    public List<ReporteDetalleLineaDto> Detalle { get; set; } = new();
}

public class ReporteVentasDto
{
    public DateTime FechaGeneracion { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public List<ReporteVentaFilaDto> Ventas { get; set; } = new();
    /// <summary>Ventas con estado REGISTRADA.</summary>
    public int CantidadVentas { get; set; }
    public int CantidadAnuladas { get; set; }
    /// <summary>Suma del total de las ventas REGISTRADAS (las anuladas no suman).</summary>
    public decimal TotalVendido { get; set; }
    /// <summary>TotalVendido / CantidadVentas.</summary>
    public decimal PromedioPorVenta { get; set; }
}
