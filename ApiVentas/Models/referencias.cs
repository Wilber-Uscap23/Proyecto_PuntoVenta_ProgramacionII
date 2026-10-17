using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiVentas.Models
{
    // Mapeos mínimos y de solo lectura que necesita el módulo de ventas.
    [Table("empleados")]
    public class EmpleadoRef
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id_empleado { get; set; }
        public string nombre { get; set; } = "";
        public string apellido { get; set; } = "";
        public bool estado { get; set; }
    }

    // De productos solo se lee el código, el precio y la existencia; la existencia se descuenta
    // al confirmar la venta.
    [Table("productos")]
    public class ProductoRef
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id_producto { get; set; }

        // Código de barras (columna "codigo" de la tabla productos, única)
        public string codigo { get; set; } = "";

        public string descripcion { get; set; } = "";
        public decimal precio_venta { get; set; }
        public int existencia { get; set; }
        public bool estado { get; set; }
    }
}