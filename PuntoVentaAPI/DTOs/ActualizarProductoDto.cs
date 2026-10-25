
using System.ComponentModel.DataAnnotations;

namespace PuntoVentaAPI.DTOs;

public class ActualizarProductoDto
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Descripcion { get; set; }

    [Range(0, 9999999999.99)]
    public decimal PrecioCosto { get; set; }

    [Range(0.01, 9999999999.99)]
    public decimal PrecioVenta { get; set; }

    [Range(0, int.MaxValue)]
    public int Existencia { get; set; }

    [Range(1, int.MaxValue)]
    public int IdMarca { get; set; }

    [MaxLength(500)]
    public string? ImagenUrl { get; set; }
}
