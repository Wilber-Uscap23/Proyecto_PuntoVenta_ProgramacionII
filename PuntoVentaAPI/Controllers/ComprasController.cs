
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace PuntoVentaAPI.Controllers;

// ============================================
// PRODUCTO EN EL DETALLE DE UNA COMPRA
// ============================================

public class DetalleCompraEntrada
{
    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    [Range(1, 1000000)]
    public int Cantidad { get; set; }

    public decimal PrecioCosto { get; set; }
}

// ============================================
// DATOS DEL ENCABEZADO Y DETALLE
// ============================================

public class GuardarCompraEntrada : IValidatableObject
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona un proveedor.")]
    public int IdProveedor { get; set; }

    [StringLength(255,
        ErrorMessage = "Las observaciones no deben superar 255 caracteres.")]
    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "El número de factura es obligatorio.")]
    [StringLength(80, MinimumLength = 1,
        ErrorMessage = "El número de factura debe tener entre 1 y 80 caracteres.")]
    public string NumeroFactura { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de factura es obligatoria.")]
    public DateOnly? FechaFactura { get; set; }

    public List<DetalleCompraEntrada> Detalles { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (Detalles == null || Detalles.Count == 0)
        {
            yield return new ValidationResult(
                "Agrega al menos un producto a la compra.",
                new[] { nameof(Detalles) });

            yield break;
        }

        if (Detalles.Count > 50)
        {
            yield return new ValidationResult(
                "Una compra puede tener como máximo 50 productos.",
                new[] { nameof(Detalles) });
        }

        if (string.IsNullOrWhiteSpace(NumeroFactura))
        {
            yield return new ValidationResult(
                "El número de factura es obligatorio.",
                new[] { nameof(NumeroFactura) });
        }

        if (FechaFactura.HasValue &&
            (FechaFactura.Value < new DateOnly(1900, 1, 1) ||
             FechaFactura.Value > DateOnly.FromDateTime(DateTime.Today)))
        {
            yield return new ValidationResult(
                "La fecha de factura debe ser válida y no puede ser futura.",
                new[] { nameof(FechaFactura) });
        }

        var repetidos = new HashSet<int>();

        foreach (var detalle in Detalles)
        {
            if (detalle.IdProducto <= 0)
            {
                yield return new ValidationResult(
                    "Selecciona un producto en cada fila.",
                    new[] { nameof(Detalles) });
            }
            else if (!repetidos.Add(detalle.IdProducto))
            {
                yield return new ValidationResult(
                    "No puedes agregar el mismo producto dos veces.",
                    new[] { nameof(Detalles) });
            }

            if (detalle.Cantidad < 1 ||
                detalle.Cantidad > 1000000)
            {
                yield return new ValidationResult(
                    "La cantidad debe estar entre 1 y 1,000,000.",
                    new[] { nameof(Detalles) });
            }

            if (detalle.PrecioCosto <= 0 ||
                detalle.PrecioCosto > 9999999999.99m ||
                decimal.Round(detalle.PrecioCosto, 2) !=
                detalle.PrecioCosto)
            {
                yield return new ValidationResult(
                    "El precio debe ser positivo y tener máximo dos decimales.",
                    new[] { nameof(Detalles) });
            }
        }
    }
}

// ============================================
// CONTROLADOR DE COMPRAS
// ============================================

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprasController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public ComprasController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // GET: LISTAR COMPRAS
    // ========================================

    [HttpGet]
    [Authorize(Roles = "ADMIN,CONSULTA")]
    public async Task<IActionResult> ObtenerCompras()
    {
        var compras = await _context.Compras
            .AsNoTracking()
            .OrderByDescending(c => c.IdCompra)
            .Select(c => new
            {
                c.IdCompra,
                c.IdProveedor,
                NombreProveedor = c.IdProveedorNavigation.Nombre,
                c.FechaCompra,
                c.NumeroFactura,
                c.FechaFactura,
                TieneFacturaPdf = c.FacturaNombre != null,
                c.Total,
                c.Observaciones,
                c.Estado,
                CantidadProductos = c.ComprasDetalles.Count,
                IdsProductos = c.ComprasDetalles
                    .Select(d => d.IdProducto)
                    .ToList()
            })
            .ToListAsync();

        return Ok(compras);
    }

    // ========================================
    // GET: CONSULTAR COMPRA Y SU DETALLE
    // ========================================

    [HttpGet("{id:int}")]
    [Authorize(Roles = "ADMIN,CONSULTA")]
    public async Task<IActionResult> ObtenerCompra(int id)
    {
        var compra = await _context.Compras
            .AsNoTracking()
            .Where(c => c.IdCompra == id)
            .Select(c => new
            {
                c.IdCompra,
                c.IdProveedor,
                NombreProveedor = c.IdProveedorNavigation.Nombre,
                c.FechaCompra,
                c.NumeroFactura,
                c.FechaFactura,
                TieneFacturaPdf = c.FacturaNombre != null,
                c.Total,
                c.Observaciones,
                c.Estado,

                Detalles = c.ComprasDetalles
                    .OrderBy(d => d.IdDetalleCompra)
                    .Select(d => new
                    {
                        d.IdDetalleCompra,
                        d.IdProducto,
                        Codigo = d.IdProductoNavigation.Codigo,
                        Descripcion = d.IdProductoNavigation.Descripcion,
                        d.Cantidad,
                        d.PrecioCosto,
                        d.Subtotal
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (compra == null)
        {
            return NotFound(new
            {
                mensaje = "La compra no existe."
            });
        }

        return Ok(compra);
    }

    // ========================================
    // PDF ORIGINAL DEL PROVEEDOR
    // ADJUNTAR Y CONSULTAR
    // ========================================

    [HttpPost("{id:int}/factura-pdf")]
    [Authorize(Roles = "ADMIN")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> AdjuntarFacturaPdf(
        int id, [FromForm] IFormFile archivo)
    {
        const int maximo = 2 * 1024 * 1024;

        if (archivo == null ||
            archivo.Length == 0 ||
            archivo.Length > maximo)
        {
            return BadRequest(new
            {
                mensaje = "Adjuntá un PDF de máximo 2 MB."
            });
        }

        var nombre = Path.GetFileName(archivo.FileName);

        if (!nombre.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            nombre.Length > 180)
        {
            return BadRequest(new
            {
                mensaje = "El archivo debe ser PDF y su nombre no debe superar 180 caracteres."
            });
        }

        byte[] bytes;

        await using (var archivoStream = archivo.OpenReadStream())
        using (var memoria = new MemoryStream())
        {
            await archivoStream.CopyToAsync(memoria);

            if (memoria.Length > maximo)
            {
                return BadRequest(new
                {
                    mensaje = "El PDF supera 2 MB."
                });
            }

            bytes = memoria.ToArray();
        }

        if (bytes.Length < 5 ||
            bytes[0] != 0x25 ||
            bytes[1] != 0x50 ||
            bytes[2] != 0x44 ||
            bytes[3] != 0x46 ||
            bytes[4] != 0x2D)
        {
            return BadRequest(new
            {
                mensaje = "El archivo no tiene una cabecera PDF válida."
            });
        }

        var compra = await _context.Compras
            .FirstOrDefaultAsync(c => c.IdCompra == id);

        if (compra == null)
        {
            return NotFound(new
            {
                mensaje = "La compra no existe."
            });
        }

        if (compra.Estado != "REGISTRADA")
        {
            return Conflict(new
            {
                mensaje = "No se puede adjuntar un PDF a una compra anulada."
            });
        }

        compra.FacturaPdf = bytes;
        compra.FacturaNombre = nombre;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Factura PDF adjuntada correctamente."
        });
    }

    [HttpGet("{id:int}/factura-pdf")]
    [Authorize(Roles = "ADMIN,CONSULTA")]
    public async Task<IActionResult> VerFacturaPdf(int id)
    {
        var factura = await _context.Compras
            .AsNoTracking()
            .Where(c => c.IdCompra == id)
            .Select(c => new
            {
                c.FacturaPdf,
                c.FacturaNombre
            })
            .FirstOrDefaultAsync();

        if (factura == null)
        {
            return NotFound(new
            {
                mensaje = "La compra no existe."
            });
        }

        if (factura.FacturaPdf == null ||
            factura.FacturaPdf.Length == 0)
        {
            return NotFound(new
            {
                mensaje = "Esta compra no tiene un PDF adjunto."
            });
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "no-store";

        return File(factura.FacturaPdf, "application/pdf");
    }

    // ========================================
    // POST: REGISTRAR COMPRA E INVENTARIO
    // ========================================

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RegistrarCompra(
        [FromBody] GuardarCompraEntrada datos)
    {
        // Bloquea operaciones concurrentes conflictivas
        // y permite deshacer todo si ocurre un error.

        await using var transaccion =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            string numeroFactura =
                datos.NumeroFactura.Trim().ToUpperInvariant();

            if (await _context.Compras.AnyAsync(c =>
                c.IdProveedor == datos.IdProveedor &&
                c.NumeroFactura == numeroFactura))
            {
                return Conflict(new
                {
                    mensaje = "Ya existe una compra con esa factura para el proveedor."
                });
            }

            bool proveedorActivo =
                await _context.Proveedores.AnyAsync(p =>
                    p.IdProveedor == datos.IdProveedor &&
                    p.Estado == true);

            if (!proveedorActivo)
            {
                return BadRequest(new
                {
                    mensaje = "Selecciona un proveedor activo."
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

            string? error = ValidarDetalle(
                datos.Detalles, productos, out decimal total);

            if (error != null)
            {
                return BadRequest(new
                {
                    mensaje = error
                });
            }

            // Revisar límites de existencias antes de guardar.

            foreach (var linea in datos.Detalles)
            {
                var producto = productos[linea.IdProducto];

                long nuevaExistencia =
                    (long)producto.Existencia + linea.Cantidad;

                if (nuevaExistencia > int.MaxValue)
                {
                    return Conflict(new
                    {
                        mensaje = $"La compra excede el límite de existencias del producto {producto.Codigo}."
                    });
                }
            }

            var compra = new Compra
            {
                IdProveedor = datos.IdProveedor,
                FechaCompra = DateTime.Now,
                NumeroFactura = numeroFactura,
                FechaFactura = datos.FechaFactura,
                Total = total,
                Observaciones = Limpiar(datos.Observaciones),
                Estado = "REGISTRADA"
            };

            _context.Compras.Add(compra);

            foreach (var linea in datos.Detalles)
            {
                decimal subtotal =
                    linea.Cantidad * linea.PrecioCosto;

                compra.ComprasDetalles.Add(new ComprasDetalle
                {
                    IdProducto = linea.IdProducto,
                    Cantidad = linea.Cantidad,
                    PrecioCosto = linea.PrecioCosto,
                    Subtotal = subtotal
                });

                // ========================================
                // ACTUALIZAR EXISTENCIA Y PRECIOS
                // ========================================

                var productoActualizado =
                    productos[linea.IdProducto];

                productoActualizado.Existencia += linea.Cantidad;

                productoActualizado.PrecioCosto =
                    linea.PrecioCosto;

                productoActualizado.PrecioVenta = decimal.Round(
                    linea.PrecioCosto * 1.25m,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            return CreatedAtAction(
                nameof(ObtenerCompra),
                new { id = compra.IdCompra },
                new
                {
                    mensaje = "Compra registrada e inventario actualizado correctamente.",
                    compra.IdCompra,
                    compra.Total,
                    compra.Estado
                });
        }
        catch (MySqlException ex)
            when (ex.Number == 1213 || ex.Number == 1205)
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
                mensaje = "No se pudo registrar la compra. No se aplicaron cambios al inventario."
            });
        }
    }

    // ========================================
    // PUT: EDITAR COMPRA REGISTRADA
    // ========================================

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActualizarCompra(
        int id,
        [FromBody] GuardarCompraEntrada datos)
    {
        await using var transaccion =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var compra = await _context.Compras
                .Include(c => c.ComprasDetalles)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null)
            {
                return NotFound(new
                {
                    mensaje = "La compra no existe."
                });
            }

            if (compra.Estado != "REGISTRADA")
            {
                return Conflict(new
                {
                    mensaje = "Solo se pueden editar compras registradas."
                });
            }

            string numeroFactura =
                datos.NumeroFactura.Trim().ToUpperInvariant();

            if (await _context.Compras.AnyAsync(c =>
                c.IdProveedor == datos.IdProveedor &&
                c.NumeroFactura == numeroFactura &&
                c.IdCompra != id))
            {
                return Conflict(new
                {
                    mensaje = "Ya existe una compra con esa factura para el proveedor."
                });
            }

            bool proveedorActivo =
                await _context.Proveedores.AnyAsync(p =>
                    p.IdProveedor == datos.IdProveedor &&
                    (p.Estado == true ||
                     p.IdProveedor == compra.IdProveedor));

            if (!proveedorActivo)
            {
                return BadRequest(new
                {
                    mensaje = "Selecciona un proveedor válido y activo."
                });
            }

            var detalleAnterior =
                compra.ComprasDetalles.ToList();

            var ids = detalleAnterior
                .Select(d => d.IdProducto)
                .Concat(datos.Detalles.Select(d => d.IdProducto))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var productos = await _context.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .OrderBy(p => p.IdProducto)
                .ToDictionaryAsync(p => p.IdProducto);

            string? error = ValidarDetalle(
                datos.Detalles, productos, out decimal total);

            if (error != null)
            {
                return BadRequest(new
                {
                    mensaje = error
                });
            }

            // Calcular el cambio NETO de inventario.

            var diferencias = new Dictionary<int, long>();

            foreach (var linea in detalleAnterior)
            {
                diferencias[linea.IdProducto] =
                    diferencias.GetValueOrDefault(
                        linea.IdProducto) - linea.Cantidad;
            }

            foreach (var linea in datos.Detalles)
            {
                diferencias[linea.IdProducto] =
                    diferencias.GetValueOrDefault(
                        linea.IdProducto) + linea.Cantidad;
            }

            foreach (var diferencia in diferencias)
            {
                if (!productos.TryGetValue(
                    diferencia.Key, out var producto))
                {
                    return Conflict(new
                    {
                        mensaje = "No se encontró un producto del detalle anterior."
                    });
                }

                long nuevaExistencia =
                    (long)producto.Existencia + diferencia.Value;

                if (nuevaExistencia < 0)
                {
                    return Conflict(new
                    {
                        mensaje = $"No puedes reducir la compra porque no hay suficientes existencias de {producto.Codigo}. Puede que esas unidades ya se hayan vendido."
                    });
                }

                if (nuevaExistencia > int.MaxValue)
                {
                    return Conflict(new
                    {
                        mensaje = $"La existencia de {producto.Codigo} excedería el límite permitido."
                    });
                }
            }

            // Actualizar inventario.

            foreach (var diferencia in diferencias)
            {
                productos[diferencia.Key].Existencia =
                    (int)((long)productos[diferencia.Key].Existencia +
                          diferencia.Value);
            }

            compra.IdProveedor = datos.IdProveedor;
            compra.NumeroFactura = numeroFactura;
            compra.FechaFactura = datos.FechaFactura;
            compra.Observaciones = Limpiar(datos.Observaciones);
            compra.Total = total;

            // Reemplazar las líneas del detalle.

            _context.ComprasDetalles.RemoveRange(detalleAnterior);

            foreach (var linea in datos.Detalles)
            {
                _context.ComprasDetalles.Add(new ComprasDetalle
                {
                    IdCompra = compra.IdCompra,
                    IdProducto = linea.IdProducto,
                    Cantidad = linea.Cantidad,
                    PrecioCosto = linea.PrecioCosto,
                    Subtotal = linea.Cantidad * linea.PrecioCosto
                });

                // ========================================
                // ACTUALIZAR PRECIOS AL EDITAR COMPRA
                // ========================================

                var productoActualizado =
                    productos[linea.IdProducto];

                productoActualizado.PrecioCosto =
                    linea.PrecioCosto;

                productoActualizado.PrecioVenta = decimal.Round(
                    linea.PrecioCosto * 1.25m,
                    2,
                    MidpointRounding.AwayFromZero);
            }

            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            return Ok(new
            {
                mensaje = "Compra actualizada e inventario ajustado correctamente.",
                compra.IdCompra,
                compra.Total,
                compra.Estado
            });
        }
        catch (MySqlException ex)
            when (ex.Number == 1213 || ex.Number == 1205)
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
                mensaje = "No se pudo actualizar la compra. Se conservaron los datos anteriores."
            });
        }
    }

    // ========================================
    // PUT: ANULAR COMPRA
    // ========================================

    [HttpPut("{id:int}/anular")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AnularCompra(int id)
    {
        await using var transaccion =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var compra = await _context.Compras
                .Include(c => c.ComprasDetalles)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null)
            {
                return NotFound(new
                {
                    mensaje = "La compra no existe."
                });
            }

            if (compra.Estado != "REGISTRADA")
            {
                return Conflict(new
                {
                    mensaje = "La compra ya fue anulada o no se encuentra registrada."
                });
            }

            var cantidades = compra.ComprasDetalles
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

            foreach (var cantidad in cantidades)
            {
                if (!productos.TryGetValue(
                    cantidad.Key, out var producto))
                {
                    return Conflict(new
                    {
                        mensaje = "No se encontró un producto de esta compra."
                    });
                }

                if (producto.Existencia < cantidad.Value)
                {
                    return Conflict(new
                    {
                        mensaje = $"No se puede anular la compra: no hay suficientes existencias de {producto.Codigo} para revertirla."
                    });
                }
            }

            foreach (var cantidad in cantidades)
            {
                productos[cantidad.Key].Existencia -=
                    (int)cantidad.Value;
            }

            compra.Estado = "ANULADA";

            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            return Ok(new
            {
                mensaje = "Compra anulada e inventario revertido correctamente.",
                compra.IdCompra,
                compra.Estado
            });
        }
        catch (MySqlException ex)
            when (ex.Number == 1213 || ex.Number == 1205)
        {
            return Conflict(new
            {
                mensaje = "El inventario está ocupado. Intenta nuevamente."
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensaje = "No se pudo anular la compra. No se modificó el inventario."
            });
        }
    }

    // ========================================
    // VALIDACIÓN CENTRAL DE DETALLES Y TOTALES
    // ========================================

    private static string? ValidarDetalle(
        List<DetalleCompraEntrada> detalles,
        Dictionary<int, Producto> productos,
        out decimal total)
    {
        total = 0m;

        if (detalles == null || detalles.Count == 0)
        {
            return "La compra debe contener al menos un producto.";
        }

        if (detalles.Count > 50)
        {
            return "Se permiten como máximo 50 productos por compra.";
        }

        var repetidos = new HashSet<int>();

        foreach (var linea in detalles)
        {
            if (!repetidos.Add(linea.IdProducto))
            {
                return "No se puede agregar el mismo producto dos veces.";
            }

            if (!productos.TryGetValue(
                linea.IdProducto, out var producto))
            {
                return "Uno de los productos seleccionados no existe.";
            }

            if (producto.Estado != true)
            {
                return $"El producto {producto.Codigo} está inactivo.";
            }

            if (linea.Cantidad < 1 ||
                linea.Cantidad > 1000000)
            {
                return $"La cantidad de {producto.Codigo} debe estar entre 1 y 1,000,000.";
            }

            if (linea.PrecioCosto <= 0 ||
                linea.PrecioCosto > 9999999999.99m ||
                decimal.Round(linea.PrecioCosto, 2) !=
                linea.PrecioCosto)
            {
                return $"El precio de {producto.Codigo} debe ser positivo y tener máximo dos decimales.";
            }

            // ========================================
            // VALIDAR PRECIO DE VENTA CALCULADO
            // ========================================

            decimal precioVentaCalculado = decimal.Round(
                linea.PrecioCosto * 1.25m,
                2,
                MidpointRounding.AwayFromZero);

            if (precioVentaCalculado > 9999999999.99m)
            {
                return $"El precio de venta calculado para {producto.Codigo} supera el límite permitido.";
            }

            decimal subtotal =
                linea.Cantidad * linea.PrecioCosto;

            if (subtotal > 9999999999.99m)
            {
                return $"El subtotal de {producto.Codigo} supera el límite permitido.";
            }

            total += subtotal;

            if (total > 9999999999.99m)
            {
                return "El total de la compra supera el límite permitido.";
            }
        }

        return null;
    }

    // ========================================
    // LIMPIAR OBSERVACIONES
    // ========================================

    private static string? Limpiar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }
}
