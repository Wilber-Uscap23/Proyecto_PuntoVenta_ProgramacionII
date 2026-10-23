using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebProductos.Models;

// Mismas reglas que ApiProductos/Dtos/ProductoDto.cs (validación en Blazor Y en la API).
public class ProductoDto
{
    public const decimal PorcentajeGanancia = 0.25m;

    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una marca.")]
    public int IdMarca { get; set; }

    public string? NombreMarca { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "El precio de costo debe ser mayor a 0.")]
    public decimal PrecioCosto { get; set; }

    public decimal PrecioVenta { get; set; }

    [Range(0, 1000000, ErrorMessage = "La existencia debe ser un número entre 0 y 1,000,000.")]
    public int Existencia { get; set; }

    [FechaValida(ErrorMessage = "La fecha de ingreso no es válida (no puede ser futura).")]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    public string? ImagenUrl { get; set; }

    public bool Estado { get; set; } = true;

    /// <summary>Vista previa en pantalla. La API siempre recalcula el valor real al guardar.</summary>
    [JsonIgnore]
    public decimal PrecioVentaCalculado =>
        Math.Round(PrecioCosto * (1 + PorcentajeGanancia), 2, MidpointRounding.AwayFromZero);
}

[AttributeUsage(AttributeTargets.Property)]
public class FechaValidaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is DateTime f && f.Date >= new DateTime(2000, 1, 1) && f.Date <= DateTime.Today;
}
