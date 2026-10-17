using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiVentas.Models
{
    // Tabla facturas. id_factura es el número FEL (autoincrementable).
    [Table("facturas")]
    public class Factura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_factura { get; set; }

        public int id_venta { get; set; }
        public DateTime fecha_emision { get; set; } = DateTime.Now;
        public string nit_comprador { get; set; } = "CF";
        public string nombre_comprador { get; set; } = "";
        public decimal total { get; set; }
    }

    // Lo que la API entrega para dibujar la factura (no es una tabla).
    public class FacturaLinea
    {
        public string descripcion { get; set; } = "";
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
    }

    public class FacturaDocumento
    {
        public int numero_fel { get; set; }
        public string serie { get; set; } = "A";
        public DateTime fecha_emision { get; set; }

        public string emisor_nombre { get; set; } = "";
        public string emisor_nit { get; set; } = "";
        public string emisor_direccion { get; set; } = "";

        public string comprador_nit { get; set; } = "CF";
        public string comprador_nombre { get; set; } = "";

        public int id_venta { get; set; }
        public string? observaciones { get; set; }

        public List<FacturaLinea> Detalles { get; set; } = new();
        public int total_unidades { get; set; }
        public decimal descuento { get; set; }
        public decimal total { get; set; }
        public string total_letras { get; set; } = "";
    }
}