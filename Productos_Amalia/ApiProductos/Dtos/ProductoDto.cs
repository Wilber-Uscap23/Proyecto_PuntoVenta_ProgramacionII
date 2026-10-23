using System.ComponentModel.DataAnnotations;

namespace ApiProductos.Dtos;

public class ProductoDto
{
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres.")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una marca.")]
    public int IdMarca { get; set; }

    /// <summary>Solo lectura: la API lo llena al consultar.</summary>
    public string? NombreMarca { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "El precio de costo debe ser mayor a 0.")]
    public decimal PrecioCosto { get; set; }

    /// <summary>La API lo calcula siempre: precio de costo + 25 %. Lo que se envíe aquí se ignora.</summary>
    public decimal PrecioVenta { get; set; }

    [Range(0, 1000000, ErrorMessage = "La existencia debe ser un número entre 0 y 1,000,000.")]
    public int Existencia { get; set; }

    [FechaValida(ErrorMessage = "La fecha de ingreso no es válida (no puede ser futura).")]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    /// <summary>Solo lectura: se modifica únicamente con POST api/productos/{id}/imagen.</summary>
    public string? ImagenUrl { get; set; }

    public bool Estado { get; set; } = true;
}

/// <summary>Valida que la fecha sea real (posterior al año 2000) y no esté en el futuro.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class FechaValidaAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is DateTime f && f.Date >= new DateTime(2000, 1, 1) && f.Date <= DateTime.Today;
}
