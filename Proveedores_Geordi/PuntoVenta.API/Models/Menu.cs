// Módulo: Menú dinámico — Rodrigo Rodríguez
// Mapea la tabla `menu` del script de Wilber (relación padre-hijo consigo misma).
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoVenta.API.Models;

[Table("menu")]
public class Menu
{
    [Key]
    [Column("id_menu")]
    public int IdMenu { get; set; }

    [Required, MaxLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Ruta de la página en Blazor. NULL cuando la opción solo agrupa a otras.</summary>
    [MaxLength(255)]
    [Column("ruta")]
    public string? Ruta { get; set; }

    /// <summary>Clase de Bootstrap Icons, por ejemplo "bi-box-seam".</summary>
    [MaxLength(100)]
    [Column("icono")]
    public string? Icono { get; set; }

    /// <summary>Opción padre. NULL = opción de primer nivel.</summary>
    [Column("id_menu_padre")]
    public int? IdMenuPadre { get; set; }

    [Column("orden")]
    public int Orden { get; set; }

    [Column("estado", TypeName = "tinyint(1)")]
    public bool Estado { get; set; } = true;

    [ForeignKey(nameof(IdMenuPadre))]
    [InverseProperty(nameof(Hijos))]
    public Menu? MenuPadre { get; set; }

    [InverseProperty(nameof(MenuPadre))]
    public ICollection<Menu> Hijos { get; set; } = new List<Menu>();
}
