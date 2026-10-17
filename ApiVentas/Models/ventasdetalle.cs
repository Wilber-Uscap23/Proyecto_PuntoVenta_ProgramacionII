using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiVentas.Models
{
    [Table("ventas_detalle")]
    public class VentasDetalle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_detalle_venta { get; set; }

        public int id_venta { get; set; }
        public int id_producto { get; set; }
        public int cantidad { get; set; }
        public decimal precio_venta { get; set; }   // en MySQL la columna se llama precio_venta
        public decimal subtotal { get; set; }

        [JsonIgnore]
        public Venta? Venta { get; set; }
    }
}