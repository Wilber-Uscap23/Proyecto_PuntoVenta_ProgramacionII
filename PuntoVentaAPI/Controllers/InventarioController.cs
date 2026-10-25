
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;

namespace PuntoVentaAPI.Controllers;

// ============================================
// MODELO PARA MOSTRAR OPERACIONES DE INVENTARIO
// ============================================
public class OperacionInventarioDto
{
    public string Tipo { get; set; } = string.Empty;

    public int IdDocumento { get; set; }

    public DateTime Fecha { get; set; }

    public string Tercero { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public int EfectoInventario { get; set; }

    public string EstadoDocumento { get; set; } = string.Empty;
}

// ============================================
// CONTROLADOR DE INVENTARIO
// ============================================
[ApiController]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public InventarioController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // GET: INVENTARIO ACTUAL
    // ========================================
    [HttpGet("productos")]
    public async Task<IActionResult> ObtenerInventario()
    {
        var inventario = await _context.Productos
            .AsNoTracking()
            .OrderBy(p => p.Codigo)
            .Select(p => new
            {
                p.IdProducto,
                p.Codigo,
                p.Descripcion,
                p.IdMarca,
                NombreMarca = p.IdMarcaNavigation.Nombre,
                p.Existencia,
                p.PrecioCosto,
                p.PrecioVenta,
                p.FechaIngreso,
                p.ImagenUrl,
                p.Estado
            })
            .ToListAsync();

        return Ok(inventario);
    }

    // ========================================
    // GET: OPERACIONES DE UN PRODUCTO
    // ========================================
    [HttpGet("{id:int}/operaciones")]
    public async Task<IActionResult> ObtenerOperaciones(
        int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El identificador del producto no es válido."
            });
        }

        bool existe = await _context.Productos
            .AsNoTracking()
            .AnyAsync(p => p.IdProducto == id);

        if (!existe)
        {
            return NotFound(new
            {
                mensaje = "El producto no existe."
            });
        }

        // ====================================
        // ENTRADAS PROCEDENTES DE COMPRAS
        // ====================================
        var entradas = await _context.ComprasDetalles
            .AsNoTracking()
            .Where(d => d.IdProducto == id)
            .OrderByDescending(
                d => d.IdCompraNavigation.FechaCompra)
            .ThenByDescending(d => d.IdDetalleCompra)
            .Take(300)
            .Select(d => new OperacionInventarioDto
            {
                Tipo = "COMPRA",
                IdDocumento = d.IdCompra,

                Fecha =
                    d.IdCompraNavigation.FechaCompra,

                Tercero =
                    d.IdCompraNavigation
                        .IdProveedorNavigation.Nombre,

                Cantidad = d.Cantidad,

                PrecioUnitario = d.PrecioCosto,

                Subtotal = d.Subtotal,

                // Una compra anulada no mantiene
                // efecto neto en el inventario.
                EfectoInventario =
                    d.IdCompraNavigation.Estado == "REGISTRADA"
                        ? d.Cantidad
                        : 0,

                EstadoDocumento =
                    d.IdCompraNavigation.Estado
            })
            .ToListAsync();

        // ====================================
        // SALIDAS PROCEDENTES DE VENTAS
        // ====================================
        var salidas = await _context.VentasDetalles
            .AsNoTracking()
            .Where(d => d.IdProducto == id)
            .OrderByDescending(
                d => d.IdVentaNavigation.FechaVenta)
            .ThenByDescending(d => d.IdDetalleVenta)
            .Take(300)
            .Select(d => new OperacionInventarioDto
            {
                Tipo = "VENTA",
                IdDocumento = d.IdVenta,

                Fecha =
                    d.IdVentaNavigation.FechaVenta,

                Tercero =
                    d.IdVentaNavigation.IdClienteNavigation.Nombre
                    + " " +
                    d.IdVentaNavigation.IdClienteNavigation.Apellido,

                Cantidad = d.Cantidad,

                PrecioUnitario = d.PrecioVenta,

                Subtotal = d.Subtotal,

                // La venta registrada descuenta unidades.
                // Una venta anulada no mantiene efecto neto.
                EfectoInventario =
                    d.IdVentaNavigation.Estado == "REGISTRADA"
                        ? -d.Cantidad
                        : 0,

                EstadoDocumento =
                    d.IdVentaNavigation.Estado
            })
            .ToListAsync();

        // ====================================
        // UNIFICAR Y ORDENAR OPERACIONES
        // ====================================
        var operaciones = entradas
            .Concat(salidas)
            .OrderByDescending(o => o.Fecha)
            .ThenByDescending(o => o.IdDocumento)
            .Take(300)
            .ToList();

        return Ok(operaciones);
    }
}
