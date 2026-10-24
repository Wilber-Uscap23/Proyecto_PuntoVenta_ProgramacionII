// Módulo: Proveedores y Compras
using System.ComponentModel.DataAnnotations;

namespace PuntoVenta.Shared.Dtos.Proveedores;

/// <summary>Proveedor tal como lo devuelve la API.</summary>
public class ProveedorDto
{
    public int IdProveedor { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Nit { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool Estado { get; set; }
}

/// <summary>
/// Datos para crear o editar un proveedor. Las mismas validaciones se usan en el
/// formulario de Blazor (DataAnnotationsValidator) y en la API.
/// </summary>
public class ProveedorGuardarDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede tener más de 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El NIT no puede tener más de 20 caracteres.")]
    [RegularExpression(@"^[0-9A-Za-z\-]+$", ErrorMessage = "El NIT solo puede tener letras, números y guiones.")]
    public string? Nit { get; set; }

    [StringLength(20, ErrorMessage = "El teléfono no puede tener más de 20 caracteres.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "El teléfono no es válido (mínimo 7 dígitos).")]
    public string? Telefono { get; set; }

    [StringLength(150, ErrorMessage = "El correo no puede tener más de 150 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string? Correo { get; set; }

    [StringLength(255, ErrorMessage = "La dirección no puede tener más de 255 caracteres.")]
    public string? Direccion { get; set; }

    [StringLength(100, ErrorMessage = "El contacto no puede tener más de 100 caracteres.")]
    public string? Contacto { get; set; }

    public bool Estado { get; set; } = true;
}
