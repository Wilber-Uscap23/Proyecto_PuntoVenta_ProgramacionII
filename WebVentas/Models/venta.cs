using System.ComponentModel.DataAnnotations;

namespace WebVentas.Models
{
    public class Venta
    {
        public int id_venta { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El id del cliente debe ser mayor a 0.")]
        public int id_cliente { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El id del empleado debe ser mayor a 0.")]
        public int id_empleado { get; set; }

        public DateTime fecha_venta { get; set; } = DateTime.Now;
        public decimal total { get; set; }

        [StringLength(255, ErrorMessage = "Las observaciones no pueden pasar de 255 caracteres.")]
        public string? observaciones { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string estado { get; set; } = "REGISTRADA";

        public List<VentasDetalle> Detalles { get; set; } = new();
    }
}