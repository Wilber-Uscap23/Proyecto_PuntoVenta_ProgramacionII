using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
namespace PuntoVentaAPI.Controllers;
// ============================================
// DETALLE DE LA VENTA
// ============================================
public class DetalleVentaEntrada
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona un producto.")]
    public int IdProducto { get; set; }
    [Range(1, 1000000,
        ErrorMessage = "La cantidad debe estar entre 1 y 1,000,000.")]
    public int Cantidad { get; set; }
    public decimal PrecioVenta { get; set; }
}
// ============================================
// DATOS PARA REGISTRAR O EDITAR VENTAS
// ============================================
public class GuardarVentaEntrada : IValidatableObject
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona un cliente.")]
    public int IdCliente { get; set; }
    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona un empleado.")]
    public int IdEmpleado { get; set; }
    [StringLength(255,
        ErrorMessage = "Las observaciones no pueden superar 255 caracteres.")]
    public string? Observaciones { get; set; }
    [Required(ErrorMessage = "Agrega productos a la venta.")]
    public List<DetalleVentaEntrada> Detalles { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (Detalles == null || Detalles.Count == 0)
        {
            yield return new ValidationResult(
                "Agrega al menos un producto.",
                new[] { nameof(Detalles) });
            yield break;
        }
        if (Detalles.Count > 50)
        {
            yield return new ValidationResult(
                "No se permiten más de 50 productos por venta.",
                new[] { nameof(Detalles) });
        }
        var ids = new HashSet<int>();
        decimal total = 0m;
        foreach (var linea in Detalles)
        {
            if (linea.IdProducto <= 0)
            {
                yield return new ValidationResult(
                    "Selecciona un producto en cada fila.",
                    new[] { nameof(Detalles) });
            }
            else if (!ids.Add(linea.IdProducto))
            {
                yield return new ValidationResult(
                    "No agregues el mismo producto dos veces.",
                    new[] { nameof(Detalles) });
            }
            if (linea.Cantidad < 1 ||
                linea.Cantidad > 1000000)
            {
                yield return new ValidationResult(
                    "La cantidad debe estar entre 1 y 1,000,000.",
                    new[] { nameof(Detalles) });
            }
            if (linea.PrecioVenta <= 0 ||
                linea.PrecioVenta > 9999999999.99m ||
                decimal.Round(linea.PrecioVenta, 2) !=
                    linea.PrecioVenta)
            {
                yield return new ValidationResult(
                    "El precio de venta debe ser positivo y tener máximo dos decimales.",
                    new[] { nameof(Detalles) });
            }
            if (linea.Cantidad > 0 && linea.PrecioVenta > 0)
            {
                decimal subtotal =
                    linea.Cantidad * linea.PrecioVenta;
                if (subtotal > 9999999999.99m)
                {
                    yield return new ValidationResult(
                        "Uno de los subtotales supera el límite permitido.",
                        new[] { nameof(Detalles) });
                }
                total += subtotal;
            }
        }
        if (total > 9999999999.99m)
        {
            yield return new ValidationResult(
                "El total supera el límite permitido.",
                new[] { nameof(Detalles) });
        }
    }
}
// ============================================
// CONTROLADOR DE VENTAS
// ============================================
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,VENDEDOR,CONSULTA")]
public class VentasController : ControllerBase
{
    private const decimal LimiteVentaCF = 2500.00m;
    private readonly PuntoVentaDbContext _context;
    public VentasController(PuntoVentaDbContext context)
    {
        _context = context;
    }
    // ========================================
    // GET: LISTADO DE VENTAS
    // ========================================
    [HttpGet]
    public async Task<IActionResult> ObtenerVentas()
    {
        var ventas = await _context.Ventas
            .AsNoTracking()
            .OrderByDescending(v => v.IdVenta)
            .Select(v => new
            {
                v.IdVenta,
                v.IdCliente,
                NombreCliente = v.IdClienteNavigation.Nombre +
                    " " + v.IdClienteNavigation.Apellido,
                v.IdEmpleado,
                NombreEmpleado = v.IdEmpleadoNavigation.Nombre +
                    " " + v.IdEmpleadoNavigation.Apellido,
                v.FechaVenta,
                v.Total,
                v.Observaciones,
                v.Estado,
                CantidadProductos = v.VentasDetalles.Count
            })
            .ToListAsync();
        return Ok(ventas);
    }
    // ========================================
    // GET: CONSULTAR VENTA CON SU DETALLE
    // ========================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerVenta(int id)
    {
        var venta = await _context.Ventas
            .AsNoTracking()
            .Where(v => v.IdVenta == id)
            .Select(v => new
            {
                v.IdVenta,
                v.IdCliente,
                NombreCliente = v.IdClienteNavigation.Nombre +
                    " " + v.IdClienteNavigation.Apellido,
                v.IdEmpleado,
                NombreEmpleado = v.IdEmpleadoNavigation.Nombre +
                    " " + v.IdEmpleadoNavigation.Apellido,
                v.FechaVenta,
                v.Total,
                v.Observaciones,
                v.Estado,
                Detalles = v.VentasDetalles
                    .OrderBy(d => d.IdDetalleVenta)
                    .Select(d => new
                    {
                        d.IdDetalleVenta,
                        d.IdProducto,
                        Codigo = d.IdProductoNavigation.Codigo,
                        Descripcion = d.IdProductoNavigation.Descripcion,
                        d.Cantidad,
                        d.PrecioVenta,
                        d.Subtotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
        if (venta == null)
        {
            return NotFound(new
            {
                mensaje = "La venta no existe."
            });
        }
        return Ok(venta);
    }
    // ========================================
    // POST: REGISTRAR VENTA
    // ========================================
    [HttpPost]
    [Authorize(Roles = "ADMIN,VENDEDOR")]
    public async Task<IActionResult> RegistrarVenta(
        [FromBody] GuardarVentaEntrada datos)
    {
        await using var transaccion =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);
        try
        {
            // El vendedor NO puede decidir a qué empleado atribuir la venta.
            // Su idEmpleado se obtiene del usuario autenticado en MySQL.
            if (User.IsInRole("VENDEDOR"))
            {
                int? idEmpleadoSesion = await ObtenerEmpleadoDeSesionAsync();
                if (!idEmpleadoSesion.HasValue)
                {
                    return Forbid();
                }

                datos.IdEmpleado = idEmpleadoSesion.Value;
            }

            string? errorPersonas = await ValidarPersonas(
                datos.IdCliente, datos.IdEmpleado);
            if (errorPersonas != null)
            {
                return BadRequest(new
                {
                    mensaje = errorPersonas
                });
            }
            var ids = datos.Detalles
                .Select(d => d.IdProducto)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            var productos = await _context.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .OrderBy(p => p.IdProducto)
                .ToDictionaryAsync(p => p.IdProducto);

            // El precio enviado por el navegador NO es confiable.
            // Para VENDEDOR siempre se usa el precio vigente en MySQL.
            // ADMIN conserva la posibilidad de editar el precio de venta.
            if (User.IsInRole("VENDEDOR"))
            {
                foreach (var linea in datos.Detalles)
                {
                    if (productos.TryGetValue(linea.IdProducto, out var producto))
                    {
                        linea.PrecioVenta = producto.PrecioVenta;
                    }
                }
            }

            string? errorDetalle = ValidarDetalle(
                datos.Detalles,
                productos,
                null,
                out decimal total);
            if (errorDetalle != null)
            {
                return BadRequest(new
                {
                    mensaje = errorDetalle
                });
            }
            // Regla de negocio aplicada en la API: evita registrar CF > Q2,500
            // incluso si intentan omitir la validación de la pantalla.
            if (total > LimiteVentaCF &&
                await EsClienteConsumidorFinalAsync(datos.IdCliente))
            {
                return BadRequest(new
                {
                    mensaje = "No se permiten ventas mayores de Q2,500.00 a Consumidor Final (CF). Seleccioná o registrá un cliente con NIT válido."
                });
            }

            // COMPROBAR EXISTENCIAS
            foreach (var linea in datos.Detalles)
            {
                var producto = productos[linea.IdProducto];
                if (producto.Existencia < linea.Cantidad)
                {
                    return Conflict(new
                    {
                        mensaje = $"No hay existencias suficientes de {producto.Codigo}. Disponibles: {producto.Existencia}."
                    });
                }
            }
            // CREAR ENCABEZADO
            var venta = new Venta
            {
                IdCliente = datos.IdCliente,
                IdEmpleado = datos.IdEmpleado,
                FechaVenta = DateTime.Now,
                Total = total,
                Observaciones = Limpiar(datos.Observaciones),
                Estado = "REGISTRADA"
            };
            _context.Ventas.Add(venta);
            // CREAR DETALLE Y DESCONTAR INVENTARIO
            foreach (var linea in datos.Detalles)
            {
                venta.VentasDetalles.Add(new VentasDetalle
                {
                    IdProducto = linea.IdProducto,
                    Cantidad = linea.Cantidad,
                    PrecioVenta = linea.PrecioVenta,
                    Subtotal = linea.Cantidad * linea.PrecioVenta
                });
                productos[linea.IdProducto].Existencia -=
                    linea.Cantidad;
            }
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return CreatedAtAction(
                nameof(ObtenerVenta),
                new { id = venta.IdVenta },
                new
                {
                    mensaje = "Venta registrada e inventario actualizado correctamente.",
                    venta.IdVenta,
                    venta.Total,
                    venta.Estado
                });
        }
        catch (Exception ex) when (EsBloqueo(ex))
        {
            return Conflict(new
            {
                mensaje = "Otra operación está actualizando el inventario. Intenta nuevamente."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensaje = "No fue posible registrar la venta. No se descontó inventario."
            });
        }
    }
    // ========================================
    // PUT: EDITAR VENTA
    // ========================================
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActualizarVenta(
        int id,
        [FromBody] GuardarVentaEntrada datos)
    {
        await using var transaccion =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);
        try
        {
            var venta = await _context.Ventas
                .Include(v => v.VentasDetalles)
                .FirstOrDefaultAsync(v => v.IdVenta == id);
            if (venta == null)
            {
                return NotFound(new
                {
                    mensaje = "La venta no existe."
                });
            }
            if (venta.Estado != "REGISTRADA")
            {
                return Conflict(new
                {
                    mensaje = "Solo se pueden editar ventas registradas."
                });
            }
            string? errorPersonas = await ValidarPersonas(
                datos.IdCliente,
                datos.IdEmpleado,
                venta.IdCliente,
                venta.IdEmpleado);
            if (errorPersonas != null)
            {
                return BadRequest(new
                {
                    mensaje = errorPersonas
                });
            }
            var detalleAnterior =
                venta.VentasDetalles.ToList();
            var cantidadesAnteriores = detalleAnterior
                .GroupBy(d => d.IdProducto)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => (long)d.Cantidad));
            var ids = detalleAnterior
                .Select(d => d.IdProducto)
                .Concat(datos.Detalles.Select(d => d.IdProducto))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            var productos = await _context.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .OrderBy(p => p.IdProducto)
                .ToDictionaryAsync(p => p.IdProducto);
            string? errorDetalle = ValidarDetalle(
                datos.Detalles,
                productos,
                cantidadesAnteriores,
                out decimal total);
            if (errorDetalle != null)
            {
                return BadRequest(new
                {
                    mensaje = errorDetalle
                });
            }
            // La misma regla rige al editar ventas desde ADMIN.
            if (total > LimiteVentaCF &&
                await EsClienteConsumidorFinalAsync(datos.IdCliente))
            {
                return BadRequest(new
                {
                    mensaje = "No se permiten ventas mayores de Q2,500.00 a Consumidor Final (CF). Seleccioná o registrá un cliente con NIT válido."
                });
            }

            // CALCULAR DIFERENCIA NETA DE INVENTARIO
            // POSITIVO = DEVOLVER UNIDADES
            // NEGATIVO = DESCONTAR UNIDADES
            var diferencias = new Dictionary<int, long>();
            foreach (var anterior in detalleAnterior)
            {
                diferencias[anterior.IdProducto] =
                    diferencias.GetValueOrDefault(
                        anterior.IdProducto) + anterior.Cantidad;
            }
            foreach (var nuevo in datos.Detalles)
            {
                diferencias[nuevo.IdProducto] =
                    diferencias.GetValueOrDefault(
                        nuevo.IdProducto) - nuevo.Cantidad;
            }
            // VALIDAR ANTES DE CAMBIAR
            foreach (var diferencia in diferencias)
            {
                if (!productos.TryGetValue(
                    diferencia.Key, out var producto))
                {
                    return Conflict(new
                    {
                        mensaje = "Un producto de la venta anterior ya no existe."
                    });
                }
                long nuevaExistencia =
                    (long)producto.Existencia + diferencia.Value;
                if (nuevaExistencia < 0)
                {
                    return Conflict(new
                    {
                        mensaje = $"No hay suficientes existencias de {producto.Codigo} para modificar la venta."
                    });
                }
                if (nuevaExistencia > int.MaxValue)
                {
                    return Conflict(new
                    {
                        mensaje = $"El inventario de {producto.Codigo} superaría el límite permitido."
                    });
                }
            }
            // AJUSTAR EXISTENCIAS
            foreach (var diferencia in diferencias)
            {
                var producto = productos[diferencia.Key];
                producto.Existencia = (int)(
                    (long)producto.Existencia + diferencia.Value);
            }
            // ACTUALIZAR ENCABEZADO
            venta.IdCliente = datos.IdCliente;
            venta.IdEmpleado = datos.IdEmpleado;
            venta.Observaciones = Limpiar(datos.Observaciones);
            venta.Total = total;
            // REEMPLAZAR EL DETALLE
            _context.VentasDetalles.RemoveRange(
                detalleAnterior);
            foreach (var linea in datos.Detalles)
            {
                _context.VentasDetalles.Add(new VentasDetalle
                {
                    IdVenta = venta.IdVenta,
                    IdProducto = linea.IdProducto,
                    Cantidad = linea.Cantidad,
                    PrecioVenta = linea.PrecioVenta,
                    Subtotal = linea.Cantidad * linea.PrecioVenta
                });
            }
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return Ok(new
            {
                mensaje = "Venta actualizada e inventario ajustado correctamente.",
                venta.IdVenta,
                venta.Total,
                venta.Estado
            });
        }
        catch (Exception ex) when (EsBloqueo(ex))
        {
            return Conflict(new
            {
                mensaje = "El inventario está siendo actualizado. Intenta nuevamente."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensaje = "No se pudo actualizar la venta. Los cambios fueron revertidos."
            });
        }
    }
    // ========================================
    // PUT: ANULAR VENTA
    // ========================================
    [HttpPut("{id:int}/anular")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AnularVenta(int id)
    {
        await using var transaccion =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);
        try
        {
            var venta = await _context.Ventas
                .Include(v => v.VentasDetalles)
                .FirstOrDefaultAsync(v => v.IdVenta == id);
            if (venta == null)
            {
                return NotFound(new
                {
                    mensaje = "La venta no existe."
                });
            }
            if (venta.Estado != "REGISTRADA")
            {
                return Conflict(new
                {
                    mensaje = "Esta venta ya fue anulada o no está registrada."
                });
            }
            var cantidades = venta.VentasDetalles
                .GroupBy(d => d.IdProducto)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => (long)d.Cantidad));
            var ids = cantidades.Keys
                .OrderBy(x => x)
                .ToList();
            var productos = await _context.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .OrderBy(p => p.IdProducto)
                .ToDictionaryAsync(p => p.IdProducto);
            // VERIFICAR LÍMITE ANTES DE DEVOLVER
            foreach (var cantidad in cantidades)
            {
                if (!productos.TryGetValue(
                    cantidad.Key, out var producto))
                {
                    return Conflict(new
                    {
                        mensaje = "No se encontró un producto de esta venta."
                    });
                }
                long nuevaExistencia =
                    (long)producto.Existencia + cantidad.Value;
                if (nuevaExistencia > int.MaxValue)
                {
                    return Conflict(new
                    {
                        mensaje = $"No se puede devolver el inventario de {producto.Codigo}: excedería el límite permitido."
                    });
                }
            }
            // DEVOLVER UNIDADES AL INVENTARIO
            foreach (var cantidad in cantidades)
            {
                productos[cantidad.Key].Existencia =
                    (int)((long)productos[cantidad.Key].Existencia +
                          cantidad.Value);
            }
            venta.Estado = "ANULADA";
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
            return Ok(new
            {
                mensaje = "Venta anulada. Las unidades fueron devueltas al inventario.",
                venta.IdVenta,
                venta.Estado
            });
        }
        catch (Exception ex) when (EsBloqueo(ex))
        {
            return Conflict(new
            {
                mensaje = "Otra operación está actualizando el inventario. Intenta nuevamente."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensaje = "No se pudo anular la venta. No se modificó el inventario."
            });
        }
    }
    // ========================================
    // IDENTIFICAR AL EMPLEADO QUE INICIÓ SESIÓN
    // ========================================
    private async Task<int?> ObtenerEmpleadoDeSesionAsync()
    {
        // En esta aplicación el ClaimTypes.NameIdentifier contiene
        // el idUsuario del JWT, no el idEmpleado del formulario.
        string? identificador = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(identificador, out int idUsuario) || idUsuario <= 0)
        {
            return null;
        }

        // La relación usuario -> empleado se consulta en MySQL.
        // Se verifican también los tres estados de la cuenta.
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario &&
                        u.Estado == true &&
                        u.Rol == "VENDEDOR" &&
                        u.IdEmpleadoNavigation.Estado == true &&
                        u.IdEmpleadoNavigation.IdPuestoNavigation.Estado == true)
            .Select(u => (int?)u.IdEmpleado)
            .FirstOrDefaultAsync();
    }

    // ========================================
    // VALIDAR CLIENTE Y EMPLEADO
    // ========================================
    // CF se identifica usando el registro genérico real de Clientes, no un valor
    // enviado por el navegador. La validación aplica para ADMIN y VENDEDOR.
    private async Task<bool> EsClienteConsumidorFinalAsync(int idCliente)
    {
        var cliente = await _context.Clientes.AsNoTracking()
            .Where(c => c.IdCliente == idCliente)
            .Select(c => new { c.Nombre, c.Apellido })
            .FirstOrDefaultAsync();

        return cliente != null &&
            string.Equals(cliente.Nombre?.Trim(), "Consumidor", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(cliente.Apellido?.Trim(), "Final", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> ValidarPersonas(
        int idCliente,
        int idEmpleado,
        int? clienteAnterior = null,
        int? empleadoAnterior = null)
    {
        bool clienteValido =
            await _context.Clientes.AnyAsync(c =>
                c.IdCliente == idCliente &&
                (c.Estado == true ||
                 c.IdCliente == clienteAnterior));
        if (!clienteValido)
        {
            return "Selecciona un cliente activo y válido.";
        }
        bool empleadoValido =
            await _context.Empleados.AnyAsync(e =>
                e.IdEmpleado == idEmpleado &&
                ((e.Estado == true &&
                  e.IdPuestoNavigation.Estado == true) ||
                 e.IdEmpleado == empleadoAnterior));
        if (!empleadoValido)
        {
            return "Selecciona un empleado activo con un puesto activo.";
        }
        return null;
    }
    // ========================================
    // VALIDAR DETALLE Y CALCULAR TOTAL
    // ========================================
    private static string? ValidarDetalle(
        List<DetalleVentaEntrada> detalles,
        Dictionary<int, Producto> productos,
        Dictionary<int, long>? cantidadesAnteriores,
        out decimal total)
    {
        total = 0m;
        if (detalles == null || detalles.Count == 0)
            return "Agrega al menos un producto.";
        if (detalles.Count > 50)
            return "Solo se permiten 50 productos por venta.";
        var seleccionados = new HashSet<int>();
        foreach (var linea in detalles)
        {
            if (!seleccionados.Add(linea.IdProducto))
                return "No agregues un producto dos veces.";
            if (!productos.TryGetValue(
                linea.IdProducto, out var producto))
            {
                return "Uno de los productos seleccionados no existe.";
            }
            if (linea.Cantidad < 1 ||
                linea.Cantidad > 1000000)
            {
                return $"La cantidad de {producto.Codigo} debe estar entre 1 y 1,000,000.";
            }
            // EN EDICIÓN PERMITE CONSERVAR O REDUCIR
            // UN PRODUCTO QUE YA QUEDÓ INACTIVO,
            // PERO NO AUMENTAR SU CANTIDAD.
            long cantidadAnterior =
                cantidadesAnteriores?.GetValueOrDefault(
                    linea.IdProducto) ?? 0;
            if (producto.Estado != true &&
                linea.Cantidad > cantidadAnterior)
            {
                return $"El producto {producto.Codigo} está inactivo.";
            }
            if (linea.PrecioVenta <= 0 ||
                linea.PrecioVenta > 9999999999.99m ||
                decimal.Round(linea.PrecioVenta, 2) !=
                    linea.PrecioVenta)
            {
                return $"El precio de {producto.Codigo} debe ser positivo y tener máximo dos decimales.";
            }
            decimal subtotal =
                linea.Cantidad * linea.PrecioVenta;
            if (subtotal > 9999999999.99m)
                return $"El subtotal de {producto.Codigo} supera el límite permitido.";
            total += subtotal;
            if (total > 9999999999.99m)
                return "El total de la venta supera el límite permitido.";
        }
        return null;
    }
    // ========================================
    // NORMALIZAR OBSERVACIONES
    // ========================================
    private static string? Limpiar(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? null
            : texto.Trim();
    }
    // ========================================
    // COMPROBAR CONFLICTOS DE MYSQL
    // ========================================
    private static bool EsBloqueo(Exception ex)
    {
        Exception? actual = ex;
        while (actual != null)
        {
            if (actual is MySqlException mysql &&
                (mysql.Number == 1213 ||
                 mysql.Number == 1205))
                return true;
            actual = actual.InnerException;
        }
        return false;
    }
}
