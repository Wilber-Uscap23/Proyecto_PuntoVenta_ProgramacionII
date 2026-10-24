// Módulo: Proveedores y Compras
namespace PuntoVenta.Shared.Dtos.Compras;

/// <summary>Una línea de la compra que envía la pantalla (el detalle).</summary>
public class CompraLineaCrearDto
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioCosto { get; set; }
}

/// <summary>Compra completa (maestro + detalle) que se guarda en UNA sola transacción.</summary>
public class CompraCrearDto
{
    public int IdProveedor { get; set; }
    public string? Observaciones { get; set; }
    public List<CompraLineaCrearDto> Detalles { get; set; } = [];
}

/// <summary>Fila del listado de compras.</summary>
public class CompraListadoDto
{
    public int IdCompra { get; set; }
    public DateTime Fecha { get; set; }
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public int CantidadArticulos { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class CompraLineaDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal Subtotal { get; set; }
    /// <summary>Precio de venta que quedó en el producto (costo + 25 %).</summary>
    public decimal PrecioVentaActual { get; set; }
}

/// <summary>Compra con todas sus líneas.</summary>
public class CompraDetalleDto : CompraListadoDto
{
    public string? NitProveedor { get; set; }
    public List<CompraLineaDto> Lineas { get; set; } = [];
}

/// <summary>Producto que se puede elegir al armar una compra.</summary>
public class ProductoCompraDto
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Existencia { get; set; }
}
