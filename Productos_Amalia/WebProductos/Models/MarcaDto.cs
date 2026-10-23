using System.ComponentModel.DataAnnotations;

namespace WebProductos.Models;

// Mismas reglas que ApiProductos/Dtos/MarcaDto.cs (validación en Blazor Y en la API).
public class MarcaDto
{
    public int IdMarca { get; set; }

    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
    public string? Descripcion { get; set; }

    public bool Estado { get; set; } = true;
}
