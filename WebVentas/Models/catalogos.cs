namespace WebVentas.Models
{
    // Opciones de solo lectura para el formulario de venta (las entrega api/ventas/catalogos
    // y los endpoints de api/busqueda).
    public class OpcionCliente
    {
        public int id_cliente { get; set; }
        public string nombre { get; set; } = "";
        public string apellido { get; set; } = "";
        public string? nit { get; set; }
    }

    public class OpcionEmpleado
    {
        public int id_empleado { get; set; }
        public string nombre { get; set; } = "";
        public string apellido { get; set; } = "";
    }

    public class OpcionProducto
    {
        public int id_producto { get; set; }
        public string? codigo { get; set; }          // código de barras
        public string descripcion { get; set; } = "";
        public decimal precio_venta { get; set; }
        public int existencia { get; set; }
    }

    public class CatalogosVenta
    {
        public List<OpcionCliente> Clientes { get; set; } = new();
        public List<OpcionEmpleado> Empleados { get; set; } = new();
        public List<OpcionProducto> Productos { get; set; } = new();
    }
}
