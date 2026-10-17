namespace WebVentas.Models
{
    public class VentasDetalle
    {
        public int id_detalle_venta { get; set; }
        public int id_venta { get; set; }
        public int id_producto { get; set; }
        public int cantidad { get; set; } = 1;
        public decimal precio_venta { get; set; }
        public decimal subtotal { get; set; }
    }
}