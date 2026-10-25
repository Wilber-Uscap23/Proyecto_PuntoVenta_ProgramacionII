using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace PuntoVentaAPI.Controllers;

// ============================================
// MODELOS DE LOS REPORTES
// ============================================

public class FilaVentaReporte
{
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string Empleado { get; set; } = "";
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
}

public class FilaCompraReporte
{
    public int IdCompra { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = "";
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
}

public class ProductoVendidoReporte
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public long UnidadesVendidas { get; set; }
    public decimal TotalVendido { get; set; }
}

public class InventarioFilaReporte
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Marca { get; set; } = "";
    public int Existencia { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal ValorCosto { get; set; }
    public bool Estado { get; set; }
}

public class ReporteGeneralDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }

    public int CantidadVentas { get; set; }
    public decimal TotalVentas { get; set; }

    public int CantidadCompras { get; set; }
    public decimal TotalCompras { get; set; }

    public int ProductosActivos { get; set; }
    public int ProductosAgotados { get; set; }
    public int ProductosStockBajo { get; set; }
    public decimal ValorInventario { get; set; }

    public List<FilaVentaReporte> Ventas { get; set; } = new();
    public List<FilaCompraReporte> Compras { get; set; } = new();
    public List<ProductoVendidoReporte> ProductosMasVendidos { get; set; } = new();
    public List<InventarioFilaReporte> Inventario { get; set; } = new();
}

// ============================================
// CONTROLADOR DE REPORTES
// ============================================

// ADMIN y CONSULTA pueden obtener reportes.
//
// VENDEDOR no puede consultar información
// financiera consolidada ni exportar reportes.

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,CONSULTA")]
public class ReportesController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    private const int UmbralStockBajo = 5;

    public ReportesController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // GET: REPORTE GENERAL
    // ========================================

    [HttpGet("general")]
    public async Task<IActionResult> ObtenerReporte(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        string? error = ValidarFechas(desde, hasta);

        if (error != null)
        {
            return BadRequest(new
            {
                mensaje = error
            });
        }

        var reporte = await ConstruirReporte(desde, hasta);

        return Ok(reporte);
    }

    // ========================================
    // GET: EXPORTAR REPORTE A CSV
    // ========================================

    [HttpGet("exportar")]
    public async Task<IActionResult> ExportarReporte(
        [FromQuery] string tipo,
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        string? error = ValidarFechas(desde, hasta);

        if (error != null)
        {
            return BadRequest(new
            {
                mensaje = error
            });
        }

        tipo = (tipo ?? "").Trim().ToUpperInvariant();

        if (tipo != "VENTAS" &&
            tipo != "COMPRAS" &&
            tipo != "PRODUCTOS" &&
            tipo != "INVENTARIO")
        {
            return BadRequest(new
            {
                mensaje = "El tipo de reporte no es válido."
            });
        }

        var reporte = await ConstruirReporte(desde, hasta);

        var csv = new StringBuilder();

        // Excel reconoce mejor los archivos CSV
        // cuando incluimos esta indicación inicial.

        csv.AppendLine("sep=;");

        switch (tipo)
        {
            // ====================================
            // EXPORTAR VENTAS
            // ====================================

            case "VENTAS":

                csv.AppendLine(
                    "Numero;Fecha;Cliente;Empleado;Total_Q;Estado");

                foreach (var v in reporte.Ventas)
                {
                    csv.AppendLine(string.Join(";",
                        CampoCsv(v.IdVenta.ToString()),
                        CampoCsv(v.Fecha.ToString("yyyy-MM-dd HH:mm")),
                        CampoCsv(v.Cliente),
                        CampoCsv(v.Empleado),
                        CampoCsv(DecimalCsv(v.Total)),
                        CampoCsv(v.Estado)
                    ));
                }

                break;

            // ====================================
            // EXPORTAR COMPRAS
            // ====================================

            case "COMPRAS":

                csv.AppendLine(
                    "Numero;Fecha;Proveedor;Total_Q;Estado");

                foreach (var c in reporte.Compras)
                {
                    csv.AppendLine(string.Join(";",
                        CampoCsv(c.IdCompra.ToString()),
                        CampoCsv(c.Fecha.ToString("yyyy-MM-dd HH:mm")),
                        CampoCsv(c.Proveedor),
                        CampoCsv(DecimalCsv(c.Total)),
                        CampoCsv(c.Estado)
                    ));
                }

                break;

            // ====================================
            // EXPORTAR PRODUCTOS MÁS VENDIDOS
            // ====================================

            case "PRODUCTOS":

                csv.AppendLine(
                    "Codigo;Descripcion;Unidades_Vendidas;Total_Vendido_Q");

                foreach (var p in reporte.ProductosMasVendidos)
                {
                    csv.AppendLine(string.Join(";",
                        CampoCsv(p.Codigo),
                        CampoCsv(p.Descripcion),
                        CampoCsv(p.UnidadesVendidas.ToString()),
                        CampoCsv(DecimalCsv(p.TotalVendido))
                    ));
                }

                break;

            // ====================================
            // EXPORTAR INVENTARIO
            // ====================================

            case "INVENTARIO":

                csv.AppendLine(
                    "Codigo;Descripcion;Marca;Existencia;Costo_Q;Venta_Q;Valor_Costo_Q;Estado");

                foreach (var p in reporte.Inventario)
                {
                    csv.AppendLine(string.Join(";",
                        CampoCsv(p.Codigo),
                        CampoCsv(p.Descripcion),
                        CampoCsv(p.Marca),
                        CampoCsv(p.Existencia.ToString()),
                        CampoCsv(DecimalCsv(p.PrecioCosto)),
                        CampoCsv(DecimalCsv(p.PrecioVenta)),
                        CampoCsv(DecimalCsv(p.ValorCosto)),
                        CampoCsv(p.Estado ? "Activo" : "Inactivo")
                    ));
                }

                break;
        }

        // ====================================
        // PREPARAR ARCHIVO CSV
        // ====================================

        // UTF-8 con BOM permite visualizar
        // correctamente las tildes en Excel.

        var bom = Encoding.UTF8.GetPreamble();

        var contenido = Encoding.UTF8.GetBytes(
            csv.ToString());

        byte[] archivo = new byte[
            bom.Length + contenido.Length];

        Buffer.BlockCopy(
            bom,
            0,
            archivo,
            0,
            bom.Length);

        Buffer.BlockCopy(
            contenido,
            0,
            archivo,
            bom.Length,
            contenido.Length);

        string nombre =
            $"Reporte_{tipo}_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.csv";

        return File(
            archivo,
            "text/csv; charset=utf-8",
            nombre);
    }

    // ========================================
    // VALIDAR RANGO DE FECHAS
    // ========================================

    private static string? ValidarFechas(
        DateTime desde,
        DateTime hasta)
    {
        if (desde == default || hasta == default)
        {
            return "Debes indicar una fecha inicial y una fecha final.";
        }

        if (desde.Date > hasta.Date)
        {
            return "La fecha inicial no puede ser posterior a la final.";
        }

        if (hasta.Date >= DateTime.MaxValue.Date)
        {
            return "La fecha final no es válida.";
        }

        // Limitar las consultas para evitar
        // reportes excesivamente grandes.

        if ((hasta.Date - desde.Date).TotalDays > 366)
        {
            return "Consulta un período máximo de 366 días.";
        }

        return null;
    }

    // ========================================
    // CONSTRUIR TODOS LOS REPORTES
    // ========================================

    private async Task<ReporteGeneralDto> ConstruirReporte(
        DateTime desde,
        DateTime hasta)
    {
        DateTime inicio = desde.Date;

        // El límite final es exclusivo
        // para incluir todas las horas
        // del último día.

        DateTime finExclusivo = hasta.Date.AddDays(1);

        // ====================================
        // VENTAS DEL PERÍODO
        // ====================================

        var ventas = await _context.Ventas
            .AsNoTracking()
            .Where(v =>
                v.FechaVenta >= inicio &&
                v.FechaVenta < finExclusivo)
            .OrderByDescending(v => v.FechaVenta)
            .ThenByDescending(v => v.IdVenta)
            .Select(v => new FilaVentaReporte
            {
                IdVenta = v.IdVenta,

                Fecha = v.FechaVenta,

                Cliente =
                    v.IdClienteNavigation.Nombre + " " +
                    v.IdClienteNavigation.Apellido,

                Empleado =
                    v.IdEmpleadoNavigation.Nombre + " " +
                    v.IdEmpleadoNavigation.Apellido,

                Total = v.Total,

                Estado = v.Estado
            })
            .ToListAsync();

        // ====================================
        // COMPRAS DEL PERÍODO
        // ====================================

        var compras = await _context.Compras
            .AsNoTracking()
            .Where(c =>
                c.FechaCompra >= inicio &&
                c.FechaCompra < finExclusivo)
            .OrderByDescending(c => c.FechaCompra)
            .ThenByDescending(c => c.IdCompra)
            .Select(c => new FilaCompraReporte
            {
                IdCompra = c.IdCompra,

                Fecha = c.FechaCompra,

                Proveedor = c.IdProveedorNavigation.Nombre,

                Total = c.Total,

                Estado = c.Estado
            })
            .ToListAsync();

        // ====================================
        // PRODUCTOS MÁS VENDIDOS
        // Solo considera ventas vigentes.
        // ====================================

        var productosMasVendidos =
            await _context.VentasDetalles
                .AsNoTracking()
                .Where(d =>
                    d.IdVentaNavigation.Estado == "REGISTRADA" &&
                    d.IdVentaNavigation.FechaVenta >= inicio &&
                    d.IdVentaNavigation.FechaVenta < finExclusivo)
                .GroupBy(d => new
                {
                    d.IdProducto,
                    d.IdProductoNavigation.Codigo,
                    d.IdProductoNavigation.Descripcion
                })
                .Select(g => new ProductoVendidoReporte
                {
                    IdProducto = g.Key.IdProducto,

                    Codigo = g.Key.Codigo,

                    Descripcion = g.Key.Descripcion,

                    UnidadesVendidas =
                        g.Sum(d => (long)d.Cantidad),

                    TotalVendido =
                        g.Sum(d => d.Subtotal)
                })
                .OrderByDescending(p => p.UnidadesVendidas)
                .ThenBy(p => p.Codigo)
                .Take(10)
                .ToListAsync();

        // ====================================
        // INVENTARIO ACTUAL
        // No depende del rango de fechas.
        // ====================================

        var inventario = await _context.Productos
            .AsNoTracking()
            .OrderBy(p => p.Codigo)
            .Select(p => new InventarioFilaReporte
            {
                IdProducto = p.IdProducto,

                Codigo = p.Codigo,

                Descripcion = p.Descripcion,

                Marca = p.IdMarcaNavigation.Nombre,

                Existencia = p.Existencia,

                PrecioCosto = p.PrecioCosto,

                PrecioVenta = p.PrecioVenta,

                ValorCosto = p.PrecioCosto * p.Existencia,

                Estado = p.Estado == true
            })
            .ToListAsync();

        // ====================================
        // SUMAR SOLO DOCUMENTOS REGISTRADOS
        // ====================================

        var ventasVigentes = ventas
            .Where(v => v.Estado == "REGISTRADA")
            .ToList();

        var comprasVigentes = compras
            .Where(c => c.Estado == "REGISTRADA")
            .ToList();

        var inventarioActivo = inventario
            .Where(p => p.Estado)
            .ToList();

        // ====================================
        // RESPUESTA FINAL
        // ====================================

        return new ReporteGeneralDto
        {
            Desde = inicio,

            Hasta = hasta.Date,

            CantidadVentas = ventasVigentes.Count,

            TotalVentas = ventasVigentes.Sum(v => v.Total),

            CantidadCompras = comprasVigentes.Count,

            TotalCompras = comprasVigentes.Sum(c => c.Total),

            ProductosActivos = inventarioActivo.Count,

            ProductosAgotados = inventarioActivo.Count(
                p => p.Existencia <= 0),

            ProductosStockBajo = inventarioActivo.Count(
                p => p.Existencia > 0 &&
                     p.Existencia <= UmbralStockBajo),

            ValorInventario = inventarioActivo.Sum(
                p => p.ValorCosto),

            Ventas = ventas,

            Compras = compras,

            ProductosMasVendidos = productosMasVendidos,

            Inventario = inventario
        };
    }

    // ========================================
    // PROTEGER EL CONTENIDO CSV
    // ========================================

    private static string CampoCsv(string? valor)
    {
        string texto = (valor ?? "")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        // Evitar que un nombre ingresado
        // en el sistema se interprete
        // como una fórmula de Excel.

        if (!string.IsNullOrEmpty(texto) &&
            (texto[0] == '=' ||
             texto[0] == '+' ||
             texto[0] == '-' ||
             texto[0] == '@'))
        {
            texto = "'" + texto;
        }

        return "\"" + texto.Replace("\"", "\"\"") + "\"";
    }

    // ========================================
    // FORMATO DE DECIMALES PARA CSV
    // ========================================

    private static string DecimalCsv(decimal valor)
    {
        return valor.ToString(
            "0.00",
            CultureInfo.InvariantCulture);
    }
}