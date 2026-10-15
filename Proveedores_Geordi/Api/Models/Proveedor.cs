using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComprasApp.Api.Models;

[Table("Proveedores")]
public class Proveedor
{
    [Key, Column("id_proveedor")]
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    [Column("nombre")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [StringLength(20, ErrorMessage = "El NIT no puede exceder 20 caracteres.")]
    [RegularExpression(@"^\d{4,12}-?[0-9Kk]$", ErrorMessage = "NIT inválido (ej. 1234567-8).")]
    [Column("nit")]
    public string Nit { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    [RegularExpression(@"^[0-9+\-\s]{8,20}$", ErrorMessage = "Teléfono inválido (solo números, 8 a 20 caracteres).")]
    [Column("telefono")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres.")]
    [Column("direccion")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
    [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres.")]
    [Column("correo")]
    public string Correo { get; set; } = "";
}
