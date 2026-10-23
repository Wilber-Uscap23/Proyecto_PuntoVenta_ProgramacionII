using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProductos.Models;

[Table("marcas")]
public class Marca
{
    [Key, Column("id_marca")] public int IdMarca { get; set; }
    [Required, MaxLength(100), Column("nombre")] public string Nombre { get; set; } = string.Empty;
    [MaxLength(255), Column("descripcion")] public string? Descripcion { get; set; }
    [Column("estado")] public bool Estado { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
