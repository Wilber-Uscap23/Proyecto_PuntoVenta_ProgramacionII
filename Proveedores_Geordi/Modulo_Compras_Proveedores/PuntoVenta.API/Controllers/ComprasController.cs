// Módulo: Proveedores y Compras
// API REST de compras (maestro/detalle).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.API.Data;
using PuntoVenta.API.Models;
using PuntoVenta.Shared.Dtos.Compras;
using PuntoVenta.Shared.Dtos.Reportes;

namespace PuntoVenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ComprasController(AppDbContext db) : ControllerBase
{
    /// <summary>Porcentaje que se suma al precio de costo para obtener el precio de venta.</summary>
    private const decimal MargenGanancia = 0.25m;

    private const int MaximoLineas = 200;
    private const int CantidadMaximaPorLinea = 100_000;
    private const decimal CostoMaximo = 9_999_999m;
    private const int LargoMaximoObservaciones = 255;
    private const int LargoMaximoBusqueda = 100;
    private const int MaximoResultadosProductos = 50;

    // ------------------------------------------------------------------
    //  GET api/compras?desde=&hasta=&idProveedor=
    // ------------------------------------------------------------------
    [HttpGet]
    [ProducesResponseType<List<CompraListadoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CompraListadoDto>>> Listar(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? idProveedor,
        CancellationToken ct)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
        {
            return Error("La fecha «Desde» no puede ser mayor que «Hasta».");
        }
        if (idProveedor is <= 0)
        {
            return Error("El proveedor seleccionado no es válido.");
        }

        var consulta = db.Compras.AsNoTracking().AsQueryable();

        if (desde.HasValue)
        {
            var inicio = desde.Value.Date;
            consulta = consulta.Where(c => c.FechaCompra >= inicio);
        }
        if (hasta.HasValue)
        {
            var finExclusivo = hasta.Value.Date.AddDays(1); // «hasta» incluye todo ese día
            consulta = consulta.Where(c => c.FechaCompra < finExclusivo);
        }
        if (idProveedor.HasValue)
        {
            consulta = consulta.Where(c => c.IdProveedor == idProveedor.Value);
        }

        var compras = await consulta
            .OrderByDescending(c => c.FechaCompra).ThenByDescending(c => c.IdCompra)
            .Select(c => new CompraListadoDto
            {
                IdCompra = c.IdCompra,
                Fecha = c.FechaCompra,
                IdProveedor = c.IdProveedor,
                Proveedor = c.Proveedor!.Nombre,
                CantidadArticulos = c.Detalles.Sum(d => d.Cantidad),
                Total = c.Total,
                Estado = c.Estado,
                Observaciones = c.Observaciones
            })
            .ToListAsync(ct);

        return Ok(compras);
    }

    // ------------------------------------------------------------------
    //  GET api/compras/5
    // ------------------------------------------------------------------
    [HttpGet("{id:int}")]
    [ProducesResponseType<CompraDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompraDetalleDto>> Obtener(int id, CancellationToken ct)
    {
        var compra = await ConsultarDetalleAsync(id, ct);
        return compra is null
            ? NotFound(new ErrorApiDto { Mensaje = "No se encontró la compra solicitada." })
            : Ok(compra);
    }

    // ------------------------------------------------------------------
    //  GET api/compras/productos?buscar=
    // ------------------------------------------------------------------
    /// <summary>Productos activos para elegir en la pantalla de compras (máximo 50 resultados).</summary>
    [HttpGet("productos")]
    [ProducesResponseType<List<ProductoCompraDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ProductoCompraDto>>> BuscarProductos([FromQuery] string? buscar, CancellationToken ct)
    {
        buscar = buscar?.Trim();
        if (buscar?.Length > LargoMaximoBusqueda)
        {
            return Error($"El texto a buscar no puede tener más de {LargoMaximoBusqueda} caracteres.");
        }

        var consulta = db.Productos.AsNoTracking().Where(p => p.Estado);
        if (!string.IsNullOrEmpty(buscar))
        {
            consulta = consulta.Where(p => p.Codigo.Contains(buscar) || p.Descripcion.Contains(buscar));
        }

        var productos = await consulta
            .OrderBy(p => p.Descripcion).ThenBy(p => p.Codigo)
            .Take(MaximoResultadosProductos)
            .Select(p => new ProductoCompraDto
            {
                IdProducto = p.IdProducto,
                Codigo = p.Codigo,
                Descripcion = p.Descripcion,
                Marca = p.Marca!.Nombre,
                PrecioCosto = p.PrecioCosto,
                PrecioVenta = p.PrecioVenta,
                Existencia = p.Existencia
            })
            .ToListAsync(ct);

        return Ok(productos);
    }

    // ------------------------------------------------------------------
    //  POST api/compras
    // ------------------------------------------------------------------
    /// <summary>
    /// Registra una compra completa en UNA sola transacción:
    ///   1) guarda la compra y sus líneas,
    ///   2) suma la cantidad comprada a la existencia de cada producto,
    ///   3) actualiza el precio de costo del producto con el de la compra,
    ///   4) recalcula el precio de venta = costo + 25 %.
    /// Si cualquier paso falla se hace Rollback y no queda ningún registro parcial.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CompraDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompraDetalleDto>> Crear([FromBody] CompraCrearDto dto, CancellationToken ct)
    {
        // ---------- Validaciones que no necesitan la base de datos ----------
        if (dto.IdProveedor <= 0)
        {
            return Error("Debe seleccionar un proveedor.");
        }
        if (dto.Detalles is null || dto.Detalles.Count == 0)
        {
            return Error("Debe agregar al menos un producto a la compra.");
        }
        if (dto.Detalles.Count > MaximoLineas)
        {
            return Error($"Una compra no puede tener más de {MaximoLineas} productos.");
        }

        var observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
        if (observaciones?.Length > LargoMaximoObservaciones)
        {
            return Error($"Las observaciones no pueden tener más de {LargoMaximoObservaciones} caracteres.");
        }

        var ids = dto.Detalles.Select(d => d.IdProducto).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return Error("Hay productos repetidos en la compra. Use una sola línea por producto.");
        }

        foreach (var linea in dto.Detalles)
        {
            if (linea.IdProducto <= 0)
            {
                return Error("Hay una línea con un producto no válido.");
            }
            if (linea.Cantidad <= 0 || linea.Cantidad > CantidadMaximaPorLinea)
            {
                return Error($"La cantidad de cada producto debe estar entre 1 y {CantidadMaximaPorLinea:N0}.");
            }
            if (linea.PrecioCosto <= 0 || linea.PrecioCosto > CostoMaximo)
            {
                return Error($"El precio de costo de cada producto debe ser mayor que 0 y no pasar de {CostoMaximo:N0}.");
            }
        }

        // ---------- Transacción ----------
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var proveedor = await db.Proveedores.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProveedor == dto.IdProveedor, ct);
            if (proveedor is null)
            {
                return Error("El proveedor seleccionado no existe.");
            }
            if (!proveedor.Estado)
            {
                return Error("El proveedor seleccionado está inactivo.");
            }

            // Se cargan CON seguimiento: sus cambios se guardan junto con la compra.
            var productos = await db.Productos
                .Where(p => ids.Contains(p.IdProducto))
                .ToDictionaryAsync(p => p.IdProducto, ct);

            var compra = new Compra
            {
                IdProveedor = dto.IdProveedor,
                FechaCompra = DateTime.Now,
                Observaciones = observaciones,
                Estado = EstadoMovimiento.Registrada
            };

            foreach (var linea in dto.Detalles)
            {
                if (!productos.TryGetValue(linea.IdProducto, out var producto))
                {
                    return Error($"El producto con id {linea.IdProducto} no existe.");
                }
                if (!producto.Estado)
                {
                    return Error($"El producto «{producto.Descripcion}» está inactivo y no se puede comprar.");
                }

                var costo = Math.Round(linea.PrecioCosto, 2, MidpointRounding.AwayFromZero);
                var subtotal = Math.Round(costo * linea.Cantidad, 2, MidpointRounding.AwayFromZero);

                compra.Detalles.Add(new CompraDetalle
                {
                    IdProducto = producto.IdProducto,
                    Cantidad = linea.Cantidad,
                    PrecioCosto = costo,
                    Subtotal = subtotal
                });
                compra.Total += subtotal;

                // Efecto sobre el inventario y los precios del producto.
                producto.Existencia += linea.Cantidad;
                producto.PrecioCosto = costo;
                producto.PrecioVenta = CalcularPrecioVenta(costo);
            }

            db.Compras.Add(compra);
            await db.SaveChangesAsync(ct);   // compra + detalles + productos, todo dentro de la transacción
            await transaccion.CommitAsync(ct);

            var creada = await ConsultarDetalleAsync(compra.IdCompra, ct);
            return CreatedAtAction(nameof(Obtener), new { id = compra.IdCompra }, creada);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // ------------------------------------------------------------------
    //  Auxiliares
    // ------------------------------------------------------------------
    /// <summary>Precio de venta = costo + 25 %, redondeado a 2 decimales.</summary>
    private static decimal CalcularPrecioVenta(decimal precioCosto)
        => Math.Round(precioCosto * (1 + MargenGanancia), 2, MidpointRounding.AwayFromZero);

    private Task<CompraDetalleDto?> ConsultarDetalleAsync(int id, CancellationToken ct)
        => db.Compras.AsNoTracking()
            .Where(c => c.IdCompra == id)
            .Select(c => new CompraDetalleDto
            {
                IdCompra = c.IdCompra,
                Fecha = c.FechaCompra,
                IdProveedor = c.IdProveedor,
                Proveedor = c.Proveedor!.Nombre,
                NitProveedor = c.Proveedor!.Nit,
                CantidadArticulos = c.Detalles.Sum(d => d.Cantidad),
                Total = c.Total,
                Estado = c.Estado,
                Observaciones = c.Observaciones,
                Lineas = c.Detalles
                    .OrderBy(d => d.IdDetalleCompra)
                    .Select(d => new CompraLineaDto
                    {
                        IdProducto = d.IdProducto,
                        Codigo = d.Producto!.Codigo,
                        Descripcion = d.Producto!.Descripcion,
                        Cantidad = d.Cantidad,
                        PrecioCosto = d.PrecioCosto,
                        Subtotal = d.Subtotal,
                        PrecioVentaActual = d.Producto!.PrecioVenta
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

    private BadRequestObjectResult Error(string mensaje) => BadRequest(new ErrorApiDto { Mensaje = mensaje });
}
