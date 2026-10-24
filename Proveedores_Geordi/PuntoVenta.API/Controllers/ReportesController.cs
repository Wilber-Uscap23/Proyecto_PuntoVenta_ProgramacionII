// Módulo: Reportes — Rodrigo Rodríguez
// Reportes de productos, existencias, compras y ventas.
// Solo LEEN información (no modifican ninguna tabla).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.API.Data;
using PuntoVenta.Shared.Dtos.Reportes;

namespace PuntoVenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReportesController(AppDbContext db) : ControllerBase
{
    private const int LargoMaximoBusqueda = 100;
    private const int StockMinimoPorDefecto = 5;

    // ------------------------------------------------------------------
    //  GET api/reportes/filtros
    // ------------------------------------------------------------------
    /// <summary>Listas para los combos de filtros de los reportes (solo registros activos).</summary>
    [HttpGet("filtros")]
    [ProducesResponseType<FiltrosReporteDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FiltrosReporteDto>> Filtros(CancellationToken ct)
    {
        var filtros = new FiltrosReporteDto
        {
            Marcas = await db.Marcas.AsNoTracking()
                .Where(m => m.Estado)
                .OrderBy(m => m.Nombre)
                .Select(m => new OpcionFiltroDto { Id = m.IdMarca, Nombre = m.Nombre })
                .ToListAsync(ct),

            Proveedores = await db.Proveedores.AsNoTracking()
                .Where(p => p.Estado)
                .OrderBy(p => p.Nombre)
                .Select(p => new OpcionFiltroDto { Id = p.IdProveedor, Nombre = p.Nombre })
                .ToListAsync(ct),

            Clientes = await db.Clientes.AsNoTracking()
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre).ThenBy(c => c.Apellido)
                .Select(c => new OpcionFiltroDto { Id = c.IdCliente, Nombre = c.Nombre + " " + c.Apellido })
                .ToListAsync(ct),

            Empleados = await db.Empleados.AsNoTracking()
                .Where(e => e.Estado)
                .OrderBy(e => e.Nombre).ThenBy(e => e.Apellido)
                .Select(e => new OpcionFiltroDto { Id = e.IdEmpleado, Nombre = e.Nombre + " " + e.Apellido })
                .ToListAsync(ct)
        };

        return Ok(filtros);
    }

    // ------------------------------------------------------------------
    //  GET api/reportes/productos?buscar=&idMarca=&estado=todos|activos|inactivos
    // ------------------------------------------------------------------
    /// <summary>Reporte de productos (catálogo con marca, precios y existencia).</summary>
    [HttpGet("productos")]
    [ProducesResponseType<ReporteProductosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteProductosDto>> Productos(
        [FromQuery] string? buscar,
        [FromQuery] int? idMarca,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        buscar = buscar?.Trim();
        if (buscar?.Length > LargoMaximoBusqueda)
        {
            return Error($"El texto a buscar no puede tener más de {LargoMaximoBusqueda} caracteres.");
        }
        if (idMarca is <= 0)
        {
            return Error("La marca seleccionada no es válida.");
        }

        var estadoNormalizado = (estado ?? "todos").Trim().ToLowerInvariant();
        if (estadoNormalizado is not ("todos" or "activos" or "inactivos"))
        {
            return Error("El estado debe ser: todos, activos o inactivos.");
        }

        var consulta = db.Productos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
        {
            consulta = consulta.Where(p => p.Codigo.Contains(buscar) || p.Descripcion.Contains(buscar));
        }
        if (idMarca.HasValue)
        {
            consulta = consulta.Where(p => p.IdMarca == idMarca.Value);
        }
        if (estadoNormalizado == "activos")
        {
            consulta = consulta.Where(p => p.Estado);
        }
        else if (estadoNormalizado == "inactivos")
        {
            consulta = consulta.Where(p => !p.Estado);
        }

        var filas = await consulta
            .OrderBy(p => p.Descripcion).ThenBy(p => p.Codigo)
            .Select(p => new ReporteProductoFilaDto
            {
                IdProducto = p.IdProducto,
                Codigo = p.Codigo,
                Descripcion = p.Descripcion,
                Marca = p.Marca!.Nombre,
                PrecioCosto = p.PrecioCosto,
                PrecioVenta = p.PrecioVenta,
                Existencia = p.Existencia,
                FechaIngreso = p.FechaIngreso,
                ImagenUrl = p.ImagenUrl,
                Activo = p.Estado
            })
            .ToListAsync(ct);

        foreach (var fila in filas)
        {
            fila.Ganancia = fila.PrecioVenta - fila.PrecioCosto;
        }

        return Ok(new ReporteProductosDto
        {
            FechaGeneracion = DateTime.Now,
            Productos = filas,
            TotalProductos = filas.Count,
            TotalActivos = filas.Count(f => f.Activo),
            TotalInactivos = filas.Count(f => !f.Activo)
        });
    }

    // ------------------------------------------------------------------
    //  GET api/reportes/existencias?idMarca=&stockMinimo=5&soloBajas=false
    // ------------------------------------------------------------------
    /// <summary>
    /// Reporte de existencias de los productos ACTIVOS: unidades, valor del inventario
    /// y alerta de productos agotados o con existencia baja.
    /// </summary>
    [HttpGet("existencias")]
    [ProducesResponseType<ReporteExistenciasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteExistenciasDto>> Existencias(
        [FromQuery] int? idMarca,
        [FromQuery] int? stockMinimo,
        [FromQuery] bool soloBajas,
        CancellationToken ct)
    {
        if (idMarca is <= 0)
        {
            return Error("La marca seleccionada no es válida.");
        }

        var minimo = stockMinimo ?? StockMinimoPorDefecto;
        if (minimo is < 0 or > 1_000_000)
        {
            return Error("El stock mínimo debe estar entre 0 y 1,000,000.");
        }

        var consulta = db.Productos.AsNoTracking().Where(p => p.Estado);

        if (idMarca.HasValue)
        {
            consulta = consulta.Where(p => p.IdMarca == idMarca.Value);
        }
        if (soloBajas)
        {
            consulta = consulta.Where(p => p.Existencia <= minimo);
        }

        var filas = await consulta
            .OrderBy(p => p.Existencia).ThenBy(p => p.Descripcion)
            .Select(p => new ReporteExistenciaFilaDto
            {
                IdProducto = p.IdProducto,
                Codigo = p.Codigo,
                Descripcion = p.Descripcion,
                Marca = p.Marca!.Nombre,
                Existencia = p.Existencia,
                PrecioCosto = p.PrecioCosto,
                PrecioVenta = p.PrecioVenta
            })
            .ToListAsync(ct);

        foreach (var fila in filas)
        {
            fila.ValorCosto = fila.Existencia * fila.PrecioCosto;
            fila.ValorVenta = fila.Existencia * fila.PrecioVenta;
            fila.EstadoStock = fila.Existencia <= 0
                ? EstadoStock.Agotado
                : fila.Existencia <= minimo ? EstadoStock.Bajo : EstadoStock.Disponible;
        }

        return Ok(new ReporteExistenciasDto
        {
            FechaGeneracion = DateTime.Now,
            StockMinimo = minimo,
            Productos = filas,
            TotalProductos = filas.Count,
            TotalUnidades = filas.Sum(f => f.Existencia),
            ValorTotalCosto = filas.Sum(f => f.ValorCosto),
            ValorTotalVenta = filas.Sum(f => f.ValorVenta),
            ProductosAgotados = filas.Count(f => f.EstadoStock == EstadoStock.Agotado),
            ProductosBajos = filas.Count(f => f.EstadoStock == EstadoStock.Bajo)
        });
    }

    // ------------------------------------------------------------------
    //  GET api/reportes/compras?desde=2026-10-01&hasta=2026-10-31&idProveedor=&estado=todas
    // ------------------------------------------------------------------
    /// <summary>Reporte de compras por rango de fechas, con su detalle de productos.</summary>
    [HttpGet("compras")]
    [ProducesResponseType<ReporteComprasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteComprasDto>> Compras(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? idProveedor,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        if (ValidarRango(desde, hasta) is { } errorFechas)
        {
            return Error(errorFechas);
        }
        if (idProveedor is <= 0)
        {
            return Error("El proveedor seleccionado no es válido.");
        }
        if (!TryNormalizarEstado(estado, out var estadoFiltro))
        {
            return Error("El estado debe ser: todas, REGISTRADA o ANULADA.");
        }

        var consulta = db.Compras.AsNoTracking().AsQueryable();

        if (desde.HasValue)
        {
            var inicio = desde.Value.Date;
            consulta = consulta.Where(c => c.FechaCompra >= inicio);
        }
        if (hasta.HasValue)
        {
            var finExclusivo = hasta.Value.Date.AddDays(1); // incluye todo el día "hasta"
            consulta = consulta.Where(c => c.FechaCompra < finExclusivo);
        }
        if (idProveedor.HasValue)
        {
            consulta = consulta.Where(c => c.IdProveedor == idProveedor.Value);
        }
        if (estadoFiltro is not null)
        {
            consulta = consulta.Where(c => c.Estado == estadoFiltro);
        }

        var filas = await consulta
            .OrderByDescending(c => c.FechaCompra).ThenByDescending(c => c.IdCompra)
            .Select(c => new ReporteCompraFilaDto
            {
                IdCompra = c.IdCompra,
                Fecha = c.FechaCompra,
                IdProveedor = c.IdProveedor,
                Proveedor = c.Proveedor!.Nombre,
                NitProveedor = c.Proveedor.Nit,
                Observaciones = c.Observaciones,
                Estado = c.Estado,
                Total = c.Total,
                Detalle = c.Detalles
                    .OrderBy(d => d.IdDetalleCompra)
                    .Select(d => new ReporteDetalleLineaDto
                    {
                        IdProducto = d.IdProducto,
                        Codigo = d.Producto!.Codigo,
                        Descripcion = d.Producto.Descripcion,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioCosto,
                        Subtotal = d.Subtotal
                    })
                    .ToList()
            })
            .AsSplitQuery()
            .ToListAsync(ct);

        foreach (var fila in filas)
        {
            fila.CantidadArticulos = fila.Detalle.Sum(d => d.Cantidad);
        }

        var registradas = filas.Where(f => f.Estado == EstadoMovimiento.Registrada).ToList();

        return Ok(new ReporteComprasDto
        {
            FechaGeneracion = DateTime.Now,
            Desde = desde?.Date,
            Hasta = hasta?.Date,
            Compras = filas,
            CantidadCompras = registradas.Count,
            CantidadAnuladas = filas.Count(f => f.Estado == EstadoMovimiento.Anulada),
            TotalComprado = registradas.Sum(f => f.Total)
        });
    }

    // ------------------------------------------------------------------
    //  GET api/reportes/ventas?desde=&hasta=&idCliente=&idEmpleado=&estado=todas
    // ------------------------------------------------------------------
    /// <summary>Reporte de ventas por rango de fechas, con cliente, vendedor y detalle de productos.</summary>
    [HttpGet("ventas")]
    [ProducesResponseType<ReporteVentasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReporteVentasDto>> Ventas(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? idCliente,
        [FromQuery] int? idEmpleado,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        if (ValidarRango(desde, hasta) is { } errorFechas)
        {
            return Error(errorFechas);
        }
        if (idCliente is <= 0)
        {
            return Error("El cliente seleccionado no es válido.");
        }
        if (idEmpleado is <= 0)
        {
            return Error("El empleado seleccionado no es válido.");
        }
        if (!TryNormalizarEstado(estado, out var estadoFiltro))
        {
            return Error("El estado debe ser: todas, REGISTRADA o ANULADA.");
        }

        var consulta = db.Ventas.AsNoTracking().AsQueryable();

        if (desde.HasValue)
        {
            var inicio = desde.Value.Date;
            consulta = consulta.Where(v => v.FechaVenta >= inicio);
        }
        if (hasta.HasValue)
        {
            var finExclusivo = hasta.Value.Date.AddDays(1);
            consulta = consulta.Where(v => v.FechaVenta < finExclusivo);
        }
        if (idCliente.HasValue)
        {
            consulta = consulta.Where(v => v.IdCliente == idCliente.Value);
        }
        if (idEmpleado.HasValue)
        {
            consulta = consulta.Where(v => v.IdEmpleado == idEmpleado.Value);
        }
        if (estadoFiltro is not null)
        {
            consulta = consulta.Where(v => v.Estado == estadoFiltro);
        }

        var filas = await consulta
            .OrderByDescending(v => v.FechaVenta).ThenByDescending(v => v.IdVenta)
            .Select(v => new ReporteVentaFilaDto
            {
                IdVenta = v.IdVenta,
                Fecha = v.FechaVenta,
                IdCliente = v.IdCliente,
                Cliente = v.Cliente!.Nombre + " " + v.Cliente.Apellido,
                NitCliente = v.Cliente.Nit,
                IdEmpleado = v.IdEmpleado,
                Empleado = v.Empleado!.Nombre + " " + v.Empleado.Apellido,
                Observaciones = v.Observaciones,
                Estado = v.Estado,
                Total = v.Total,
                Detalle = v.Detalles
                    .OrderBy(d => d.IdDetalleVenta)
                    .Select(d => new ReporteDetalleLineaDto
                    {
                        IdProducto = d.IdProducto,
                        Codigo = d.Producto!.Codigo,
                        Descripcion = d.Producto.Descripcion,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioVenta,
                        Subtotal = d.Subtotal
                    })
                    .ToList()
            })
            .AsSplitQuery()
            .ToListAsync(ct);

        foreach (var fila in filas)
        {
            fila.CantidadArticulos = fila.Detalle.Sum(d => d.Cantidad);
        }

        var registradas = filas.Where(f => f.Estado == EstadoMovimiento.Registrada).ToList();
        var totalVendido = registradas.Sum(f => f.Total);

        return Ok(new ReporteVentasDto
        {
            FechaGeneracion = DateTime.Now,
            Desde = desde?.Date,
            Hasta = hasta?.Date,
            Ventas = filas,
            CantidadVentas = registradas.Count,
            CantidadAnuladas = filas.Count(f => f.Estado == EstadoMovimiento.Anulada),
            TotalVendido = totalVendido,
            PromedioPorVenta = registradas.Count == 0 ? 0 : Math.Round(totalVendido / registradas.Count, 2)
        });
    }

    // ------------------------------------------------------------------
    //  Utilidades
    // ------------------------------------------------------------------

    private BadRequestObjectResult Error(string mensaje) => BadRequest(new ErrorApiDto { Mensaje = mensaje });

    private static string? ValidarRango(DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
        {
            return "La fecha 'desde' no puede ser mayor que la fecha 'hasta'.";
        }
        return null;
    }

    /// <summary>Acepta "todas"/vacío (sin filtro), "REGISTRADA" o "ANULADA" (sin importar mayúsculas).</summary>
    private static bool TryNormalizarEstado(string? estado, out string? estadoFiltro)
    {
        estadoFiltro = null;
        var valor = estado?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(valor) || valor is "TODAS" or "TODOS")
        {
            return true;
        }
        if (valor is EstadoMovimiento.Registrada or EstadoMovimiento.Anulada)
        {
            estadoFiltro = valor;
            return true;
        }
        return false;
    }
}
