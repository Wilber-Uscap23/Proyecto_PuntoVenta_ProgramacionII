namespace WebVentas.Models
{
    public class FacturaLinea
    {
        public string descripcion { get; set; } = "";
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
    }

    public class FacturaDocumento
    {
        // En la vista previa, numero_fel e id_venta valen 0 (se asignan al confirmar).
        public int numero_fel { get; set; }
        public string serie { get; set; } = "A";
        public DateTime fecha_emision { get; set; }

        public string emisor_nombre { get; set; } = "";
        public string emisor_nit { get; set; } = "";
        public string emisor_direccion { get; set; } = "";

        public string comprador_nit { get; set; } = "CF";
        public string comprador_nombre { get; set; } = "";

        public int id_venta { get; set; }

        public List<FacturaLinea> Detalles { get; set; } = new();
        public int total_unidades { get; set; }
        public decimal descuento { get; set; }
        public decimal total { get; set; }
        public string total_letras { get; set; } = "";
    }
}