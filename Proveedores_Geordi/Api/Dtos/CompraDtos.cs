using System.ComponentModel.DataAnnotations;

namespace ComprasApp.Api.Dtos;

public class CompraCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un proveedor.")]
    public int IdProveedor { get; set; }

    [Required, MinLength(1, ErrorMessage = "La compra debe tener al menos un producto.")]
    public List<CompraDetalleCreateDto> Detalles { get; set; } = new();
}

public class CompraDetalleCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Producto inválido.")]
    public int IdProducto { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; }

    [Range(0.01, 100_000_000, ErrorMessage = "El precio debe ser mayor a cero.")]
    public decimal PrecioCosto { get; set; }
}

public record CompraResumenDto(int IdCompra, DateTime Fecha, int IdProveedor, string Proveedor, decimal Total);
public record CompraDetalleDto(int IdProducto, string Producto, int Cantidad, decimal PrecioCosto, decimal Subtotal);
public record CompraResponseDto(int IdCompra, DateTime Fecha, int IdProveedor, string Proveedor, decimal Total, List<CompraDetalleDto> Detalles);
