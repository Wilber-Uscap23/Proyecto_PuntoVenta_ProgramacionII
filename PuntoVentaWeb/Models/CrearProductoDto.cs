
using System.ComponentModel.DataAnnotations;

namespace PuntoVentaWeb.Models;

public class CrearProductoDto
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Descripcion { get; set; }

    [Range(typeof(decimal), "0", "9999999999.99",
        ErrorMessage = "El precio de costo no puede ser negativo.")]
    public decimal PrecioCosto { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999.99",
        ErrorMessage = "El precio de venta debe ser mayor que cero.")]
    public decimal PrecioVenta { get; set; }

    [Range(0, int.MaxValue,
        ErrorMessage = "La existencia no puede ser negativa.")]
    public int Existencia { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Debes seleccionar una marca válida.")]
    public int IdMarca { get; set; } = 1;

    [MaxLength(500)]
    public string? ImagenUrl { get; set; }
}
