using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiVentas.Models
{
    [Table("ventas")]
    public class Venta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_venta { get; set; }

        public int id_cliente { get; set; }
        public int id_empleado { get; set; }
        public DateTime fecha_venta { get; set; } = DateTime.Now;
        public decimal total { get; set; }
        public string? observaciones { get; set; }
        public string estado { get; set; } = "REGISTRADA";

        // Relación 1 venta -> muchos detalles
        public List<VentasDetalle>? Detalles { get; set; }
    }
}