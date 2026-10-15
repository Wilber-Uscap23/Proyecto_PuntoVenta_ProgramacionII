using System.ComponentModel.DataAnnotations;

namespace ComprasApp.Client.Models;

public class Proveedor
{
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    [RegularExpression(@"^\d{4,12}-?[0-9Kk]$", ErrorMessage = "NIT inválido (ej. 1234567-8).")]
    public string Nit { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^[0-9+\-\s]{8,20}$", ErrorMessage = "Teléfono inválido (8 a 20 dígitos).")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Correo { get; set; } = "";
}

public class Producto
{
    public int IdProducto { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Existencia { get; set; }
}

public class CompraCreate
{
    public int IdProveedor { get; set; }
    public List<CompraDetalleCreate> Detalles { get; set; } = new();
}

public class CompraDetalleCreate
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioCosto { get; set; }
}

public record CompraResumen(int IdCompra, DateTime Fecha, int IdProveedor, string Proveedor, decimal Total);
public record CompraDetalleView(int IdProducto, string Producto, int Cantidad, decimal PrecioCosto, decimal Subtotal);
public record CompraView(int IdCompra, DateTime Fecha, int IdProveedor, string Proveedor, decimal Total, List<CompraDetalleView> Detalles);
