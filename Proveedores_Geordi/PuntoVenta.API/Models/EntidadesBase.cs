// =====================================================================
//  BASE DE INTEGRACIÓN — NO es parte del módulo de Rodrigo.
//  Son las entidades de Wilber, Jose, Geordi y Amalia, mapeadas EXACTAMENTE
//  a las tablas del script PuntoVentaDB.sql, para que los reportes compilen
//  y se puedan probar.
//
//  Al integrar: usen las entidades reales de sus compañeros y borren este
//  archivo. Si alguno usó nombres distintos (por ejemplo "Proveedore" o
//  "IdMarcaNavigation" si se hizo Scaffold), solo hay que cambiar esos
//  nombres en Controllers/ReportesController.cs.
// =====================================================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PuntoVenta.API.Models;

[Table("puestos")]
public class Puesto
{
    [Key, Column("id_puesto")] public int IdPuesto { get; set; }
    [Required, MaxLength(100), Column("nombre")] public string Nombre { get; set; } = string.Empty;
    [MaxLength(255), Column("descripcion")] public string? Descripcion { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}

[Table("empleados")]
public class Empleado
{
    [Key, Column("id_empleado")] public int IdEmpleado { get; set; }
    [Required, MaxLength(100), Column("nombre")] public string Nombre { get; set; } = string.Empty;
    [Required, MaxLength(100), Column("apellido")] public string Apellido { get; set; } = string.Empty;
    [MaxLength(20), Column("telefono")] public string? Telefono { get; set; }
    [MaxLength(150), Column("correo")] public string? Correo { get; set; }
    [MaxLength(255), Column("direccion")] public string? Direccion { get; set; }
    [Column("fecha_ingreso", TypeName = "date")] public DateTime FechaIngreso { get; set; }
    [Column("id_puesto")] public int IdPuesto { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;

    [ForeignKey(nameof(IdPuesto))] public Puesto? Puesto { get; set; }
}

[Table("usuarios")]
public class Usuario
{
    [Key, Column("id_usuario")] public int IdUsuario { get; set; }
    [Column("id_empleado")] public int IdEmpleado { get; set; }
    [Required, MaxLength(50), Column("usuario")] public string NombreUsuario { get; set; } = string.Empty;
    [Required, MaxLength(255), Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
    [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
    [Column("ultimo_acceso")] public DateTime? UltimoAcceso { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;

    [ForeignKey(nameof(IdEmpleado))] public Empleado? Empleado { get; set; }
}

[Table("clientes")]
public class Cliente
{
    [Key, Column("id_cliente")] public int IdCliente { get; set; }
    [Required, MaxLength(100), Column("nombre")] public string Nombre { get; set; } = string.Empty;
    [Required, MaxLength(100), Column("apellido")] public string Apellido { get; set; } = string.Empty;
    [MaxLength(20), Column("nit")] public string? Nit { get; set; }
    [MaxLength(20), Column("telefono")] public string? Telefono { get; set; }
    [MaxLength(150), Column("correo")] public string? Correo { get; set; }
    [MaxLength(255), Column("direccion")] public string? Direccion { get; set; }
    [Column("fecha_registro")] public DateTime FechaRegistro { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;
}

[Table("proveedores")]
public class Proveedor
{
    [Key, Column("id_proveedor")] public int IdProveedor { get; set; }
    [Required, MaxLength(150), Column("nombre")] public string Nombre { get; set; } = string.Empty;
    [MaxLength(20), Column("nit")] public string? Nit { get; set; }
    [MaxLength(20), Column("telefono")] public string? Telefono { get; set; }
    [MaxLength(150), Column("correo")] public string? Correo { get; set; }
    [MaxLength(255), Column("direccion")] public string? Direccion { get; set; }
    [MaxLength(100), Column("contacto")] public string? Contacto { get; set; }
    [Column("fecha_registro")] public DateTime FechaRegistro { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;
}

[Table("marcas")]
public class Marca
{
    [Key, Column("id_marca")] public int IdMarca { get; set; }
    [Required, MaxLength(100), Column("nombre")] public string Nombre { get; set; } = string.Empty;
    [MaxLength(255), Column("descripcion")] public string? Descripcion { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

[Table("productos")]
public class Producto
{
    [Key, Column("id_producto")] public int IdProducto { get; set; }
    [Required, MaxLength(50), Column("codigo")] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(255), Column("descripcion")] public string Descripcion { get; set; } = string.Empty;
    [Column("id_marca")] public int IdMarca { get; set; }
    [Precision(12, 2), Column("precio_costo")] public decimal PrecioCosto { get; set; }
    [Precision(12, 2), Column("precio_venta")] public decimal PrecioVenta { get; set; }
    [Column("existencia")] public int Existencia { get; set; }
    [Column("fecha_ingreso")] public DateTime FechaIngreso { get; set; }
    [MaxLength(500), Column("imagen_url")] public string? ImagenUrl { get; set; }
    [Column("estado", TypeName = "tinyint(1)")] public bool Estado { get; set; } = true;

    [ForeignKey(nameof(IdMarca))] public Marca? Marca { get; set; }
}

[Table("compras")]
public class Compra
{
    [Key, Column("id_compra")] public int IdCompra { get; set; }
    [Column("id_proveedor")] public int IdProveedor { get; set; }
    [Column("fecha_compra")] public DateTime FechaCompra { get; set; }
    [Precision(12, 2), Column("total")] public decimal Total { get; set; }
    [MaxLength(255), Column("observaciones")] public string? Observaciones { get; set; }
    [Required, MaxLength(20), Column("estado")] public string Estado { get; set; } = "REGISTRADA";

    [ForeignKey(nameof(IdProveedor))] public Proveedor? Proveedor { get; set; }
    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}

[Table("compras_detalle")]
public class CompraDetalle
{
    [Key, Column("id_detalle_compra")] public int IdDetalleCompra { get; set; }
    [Column("id_compra")] public int IdCompra { get; set; }
    [Column("id_producto")] public int IdProducto { get; set; }
    [Column("cantidad")] public int Cantidad { get; set; }
    [Precision(12, 2), Column("precio_costo")] public decimal PrecioCosto { get; set; }
    [Precision(12, 2), Column("subtotal")] public decimal Subtotal { get; set; }

    [ForeignKey(nameof(IdCompra))] public Compra? Compra { get; set; }
    [ForeignKey(nameof(IdProducto))] public Producto? Producto { get; set; }
}

[Table("ventas")]
public class Venta
{
    [Key, Column("id_venta")] public int IdVenta { get; set; }
    [Column("id_cliente")] public int IdCliente { get; set; }
    [Column("id_empleado")] public int IdEmpleado { get; set; }
    [Column("fecha_venta")] public DateTime FechaVenta { get; set; }
    [Precision(12, 2), Column("total")] public decimal Total { get; set; }
    [MaxLength(255), Column("observaciones")] public string? Observaciones { get; set; }
    [Required, MaxLength(20), Column("estado")] public string Estado { get; set; } = "REGISTRADA";

    [ForeignKey(nameof(IdCliente))] public Cliente? Cliente { get; set; }
    [ForeignKey(nameof(IdEmpleado))] public Empleado? Empleado { get; set; }
    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
}

[Table("ventas_detalle")]
public class VentaDetalle
{
    [Key, Column("id_detalle_venta")] public int IdDetalleVenta { get; set; }
    [Column("id_venta")] public int IdVenta { get; set; }
    [Column("id_producto")] public int IdProducto { get; set; }
    [Column("cantidad")] public int Cantidad { get; set; }
    [Precision(12, 2), Column("precio_venta")] public decimal PrecioVenta { get; set; }
    [Precision(12, 2), Column("subtotal")] public decimal Subtotal { get; set; }

    [ForeignKey(nameof(IdVenta))] public Venta? Venta { get; set; }
    [ForeignKey(nameof(IdProducto))] public Producto? Producto { get; set; }
}
