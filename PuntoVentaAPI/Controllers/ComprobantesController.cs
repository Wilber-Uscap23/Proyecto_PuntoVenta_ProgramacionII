

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using PuntoVentaAPI.Data;

using PuntoVentaAPI.Models;

using QuestPDF.Fluent;

using QuestPDF.Helpers;

using QuestPDF.Infrastructure;

using System.Globalization;

namespace PuntoVentaAPI.Controllers;

[ApiController]

[Route("api/[controller]")]

[Authorize(Roles = "ADMIN,VENDEDOR,CONSULTA")]

public class ComprobantesController : ControllerBase

{

    private readonly PuntoVentaDbContext _context;

    private static readonly CultureInfo Cultura =

        CultureInfo.GetCultureInfo("es-GT");

    public ComprobantesController(PuntoVentaDbContext context)

    {

        _context = context;

    }

    // ========================================

    // VER E IMPRIMIR EL PDF EN EL NAVEGADOR

    // GET api/Comprobantes/venta/1

    // ========================================

    [HttpGet("venta/{id:int}")]

    public async Task<IActionResult> VerComprobante(int id)

    {

        return await GenerarComprobante(id, false);

    }

    // ========================================

    // DESCARGAR EL PDF

    // GET api/Comprobantes/venta/1/descargar

    // ========================================

    [HttpGet("venta/{id:int}/descargar")]

    public async Task<IActionResult> DescargarComprobante(int id)

    {

        return await GenerarComprobante(id, true);

    }

    // ========================================

    // CONSULTAR DATOS Y GENERAR DOCUMENTO

    // ========================================

    private async Task<IActionResult> GenerarComprobante(

        int id,

        bool descargar)

    {

        if (id <= 0)

        {

            return BadRequest(new

            {

                mensaje = "El número de venta no es válido."

            });

        }

        var venta = await _context.Ventas

            .AsNoTracking()

            .Include(v => v.IdClienteNavigation)

            .Include(v => v.IdEmpleadoNavigation)

            .Include(v => v.VentasDetalles)

                .ThenInclude(d => d.IdProductoNavigation)

            .FirstOrDefaultAsync(v => v.IdVenta == id);

        if (venta == null)

        {

            return NotFound(new

            {

                mensaje = "No se encontró la venta solicitada."

            });

        }

        if (venta.VentasDetalles.Count == 0)

        {

            return Conflict(new

            {

                mensaje = "La venta no tiene productos registrados."

            });

        }

        byte[] pdf = CrearPdf(venta);

        string nombreArchivo =

            $"Comprobante_Venta_{venta.IdVenta:000000}.pdf";

        Response.Headers["Cache-Control"] = "no-store, private";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (descargar)

        {

            return File(

                pdf,

                "application/pdf",

                nombreArchivo);

        }

        // Mostrar el PDF en el navegador para imprimirlo.

        Response.Headers["Content-Disposition"] =

            $"inline; filename=\"{nombreArchivo}\"";

        return File(pdf, "application/pdf");

    }

    // ========================================

    // DISEÑO DEL COMPROBANTE DE VENTA

    // ========================================

    private static byte[] CrearPdf(Venta venta)

    {

        bool anulada = string.Equals(

            venta.Estado,

            "ANULADA",

            StringComparison.OrdinalIgnoreCase);

        string cliente =

            $"{venta.IdClienteNavigation.Nombre} " +

            $"{venta.IdClienteNavigation.Apellido}";

        string empleado =

            $"{venta.IdEmpleadoNavigation.Nombre} " +

            $"{venta.IdEmpleadoNavigation.Apellido}";

        string nit = string.IsNullOrWhiteSpace(

            venta.IdClienteNavigation.Nit)

            ? "No registrado"

            : venta.IdClienteNavigation.Nit;

        var detalles = venta.VentasDetalles

            .OrderBy(d => d.IdDetalleVenta)

            .ToList();

        var documento = Document.Create(container =>

        {

            container.Page(page =>

            {

                page.Size(PageSizes.A4);

                page.Margin(35);

                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>

                    style.FontSize(10));

                // ====================================

                // ENCABEZADO

                // ====================================

                page.Header().Column(col =>

                {

                    col.Item()

                        .Background(Colors.Blue.Darken3)

                        .Padding(18)

                        .Row(row =>

                        {

                            row.RelativeItem().Column(izquierda =>

                            {

                                izquierda.Item()

                                    .Text("SISTEMA PUNTO DE VENTA")

                                    .FontSize(18)

                                    .Bold()

                                    .FontColor(Colors.White);

                                izquierda.Item()

                                    .Text("COMPROBANTE DE VENTA")

                                    .FontSize(11)

                                    .FontColor(Colors.White);

                            });

                            row.ConstantItem(155)

                                .AlignRight()

                                .Column(derecha =>

                                {

                                    derecha.Item()

                                        .Text($"No. {venta.IdVenta:000000}")

                                        .FontSize(16)

                                        .Bold()

                                        .FontColor(Colors.White)

                                        .AlignRight();

                                    derecha.Item()

                                        .Text(

                                            venta.FechaVenta.ToString(

                                                "dd/MM/yyyy HH:mm"))

                                        .FontSize(9)

                                        .FontColor(Colors.White)

                                        .AlignRight();

                                });

                        });

                    col.Item()

                        .PaddingTop(10)

                        .AlignCenter()

                        .Text("FACTURA")

                        .FontSize(9)

                        .Bold()

                        .FontColor(Colors.Red.Darken2);

                });

                // ====================================

                // CONTENIDO

                // ====================================

                page.Content()

                    .PaddingVertical(18)

                    .Column(col =>

                    {

                        col.Spacing(16);

                        // Estado de anulación.

                        if (anulada)

                        {

                            col.Item()

                                .Background(Colors.Red.Lighten4)

                                .Padding(12)

                                .AlignCenter()

                                .Text("VENTA ANULADA")

                                .FontSize(17)

                                .Bold()

                                .FontColor(Colors.Red.Darken3);

                        }

                        // ====================================

                        // DATOS DEL CLIENTE Y EMPLEADO

                        // ====================================

                        col.Item().Row(row =>

                        {

                            row.RelativeItem().Column(datos =>

                            {

                                datos.Spacing(5);

                                datos.Item()

                                    .Text("DATOS DEL CLIENTE")

                                    .Bold()

                                    .FontColor(Colors.Blue.Darken2);

                                datos.Item().Text(

                                    $"Nombre: {cliente.Trim()}");

                                datos.Item().Text(

                                    $"NIT: {nit}");

                                if (!string.IsNullOrWhiteSpace(

                                    venta.IdClienteNavigation.Direccion))

                                {

                                    datos.Item().Text(

                                        "Dirección: " +

                                        venta.IdClienteNavigation.Direccion);

                                }

                            });

                            row.RelativeItem().Column(datos =>

                            {

                                datos.Spacing(5);

                                datos.Item()

                                    .Text("INFORMACIÓN DE LA VENTA")

                                    .Bold()

                                    .FontColor(Colors.Blue.Darken2);

                                datos.Item().Text(

                                    $"Venta: {venta.IdVenta:000000}");

                                datos.Item().Text(

                                    "Fecha: " +

                                    venta.FechaVenta.ToString(

                                        "dd/MM/yyyy HH:mm"));

                                datos.Item().Text(

                                    $"Atendido por: {empleado.Trim()}");

                                datos.Item().Text(

                                    "Estado: " +

                                    (anulada ? "ANULADA" : venta.Estado));

                            });

                        });

                        // ====================================

                        // TABLA DE PRODUCTOS

                        // ====================================

                        col.Item().Table(tabla =>

                        {

                            tabla.ColumnsDefinition(columnas =>

                            {

                                columnas.ConstantColumn(65);

                                columnas.RelativeColumn();

                                columnas.ConstantColumn(55);

                                columnas.ConstantColumn(90);

                                columnas.ConstantColumn(90);

                            });

                            tabla.Header(encabezado =>

                            {

                                encabezado.Cell()

                                    .Element(EstiloEncabezado)

                                    .Text("Código");

                                encabezado.Cell()

                                    .Element(EstiloEncabezado)

                                    .Text("Producto");

                                encabezado.Cell()

                                    .Element(EstiloEncabezado)

                                    .AlignRight()

                                    .Text("Cant.");

                                encabezado.Cell()

                                    .Element(EstiloEncabezado)

                                    .AlignRight()

                                    .Text("Precio");

                                encabezado.Cell()

                                    .Element(EstiloEncabezado)

                                    .AlignRight()

                                    .Text("Subtotal");

                            });

                            foreach (var detalle in detalles)

                            {

                                tabla.Cell()

                                    .Element(EstiloDetalle)

                                    .Text(detalle.IdProductoNavigation.Codigo);

                                tabla.Cell()

                                    .Element(EstiloDetalle)

                                    .Text(detalle.IdProductoNavigation.Descripcion);

                                tabla.Cell()

                                    .Element(EstiloDetalle)

                                    .AlignRight()

                                    .Text(detalle.Cantidad.ToString());

                                tabla.Cell()

                                    .Element(EstiloDetalle)

                                    .AlignRight()

                                    .Text(Q(detalle.PrecioVenta));

                                tabla.Cell()

                                    .Element(EstiloDetalle)

                                    .AlignRight()

                                    .Text(Q(detalle.Subtotal));

                            }

                        });

                        // ====================================

                        // TOTAL

                        // ====================================

                        col.Item()

                            .AlignRight()

                            .Width(250)

                            .Background(Colors.Blue.Lighten5)

                            .Padding(14)

                            .Row(row =>

                            {

                                row.RelativeItem()

                                    .Text("TOTAL")

                                    .Bold()

                                    .FontSize(13);

                                row.ConstantItem(130)

                                    .AlignRight()

                                    .Text(Q(venta.Total))

                                    .FontSize(16)

                                    .Bold()

                                    .FontColor(Colors.Blue.Darken3);

                            });

                        // ====================================

                        // OBSERVACIONES

                        // ====================================

                        if (!string.IsNullOrWhiteSpace(

                            venta.Observaciones))

                        {

                            col.Item().Column(datos =>

                            {

                                datos.Spacing(5);

                                datos.Item()

                                    .Text("OBSERVACIONES")

                                    .Bold()

                                    .FontColor(Colors.Blue.Darken2);

                                datos.Item().Text(venta.Observaciones);

                            });

                        }

                        col.Item()

                            .PaddingTop(15)

                            .AlignCenter()

                            .Text("¡Gracias por su compra!")

                            .Bold()

                            .FontSize(12);

                        col.Item()

                            .AlignCenter()

                            .Text(

                                "Este comprobante es únicamente un respaldo " +

                                "interno de la venta registrada en el sistema.")

                            .FontSize(9)

                            .FontColor(Colors.Grey.Darken2)

                            .AlignCenter();

                    });

                // ====================================

                // PIE DE PÁGINA

                // ====================================

                page.Footer()

                    .BorderTop(1)

                    .BorderColor(Colors.Grey.Lighten2)

                    .PaddingTop(9)

                    .AlignCenter()

                    .Text(texto =>

                    {

                        texto.Span("Sistema Punto de Venta | Documento no fiscal | Página ");

                        texto.CurrentPageNumber();

                        texto.Span(" de ");

                        texto.TotalPages();

                    });

            });

        });

        return documento.GeneratePdf();

    }

    // ========================================

    // ESTILOS DE LA TABLA

    // ========================================

    private static IContainer EstiloEncabezado(

        IContainer container)

    {

        return container

            .Background(Colors.Blue.Darken2)

            .DefaultTextStyle(estilo =>

                estilo.FontColor(Colors.White)

                      .Bold()

                      .FontSize(9))

            .PaddingVertical(9)

            .PaddingHorizontal(6);

    }

    private static IContainer EstiloDetalle(

        IContainer container)

    {

        return container

            .BorderBottom(1)

            .BorderColor(Colors.Grey.Lighten2)

            .PaddingVertical(10)

            .PaddingHorizontal(6);

    }

    // ========================================

    // FORMATO EN QUETZALES

    // ========================================

    private static string Q(decimal cantidad)

    {

        return "Q " + cantidad.ToString("N2", Cultura);

    }

}
