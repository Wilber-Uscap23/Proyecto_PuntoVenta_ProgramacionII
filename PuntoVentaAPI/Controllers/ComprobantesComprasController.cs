
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
[Route("api/Comprobantes/compra")]
[Authorize(Roles = "ADMIN,CONSULTA")]
public class ComprobantesComprasController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-GT");

    public ComprobantesComprasController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // VER EL COMPROBANTE EN EL NAVEGADOR
    [HttpGet("{id:int}")]
    public async Task<IActionResult> VerComprobante(int id)
    {
        return await GenerarComprobante(id, false);
    }

    // DESCARGAR EL COMPROBANTE
    [HttpGet("{id:int}/descargar")]
    public async Task<IActionResult> DescargarComprobante(int id)
    {
        return await GenerarComprobante(id, true);
    }

    private async Task<IActionResult> GenerarComprobante(
        int id, bool descargar)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El número de compra no es válido."
            });
        }

        var compra = await _context.Compras
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.ComprasDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .FirstOrDefaultAsync(c => c.IdCompra == id);

        if (compra == null)
        {
            return NotFound(new
            {
                mensaje = "No se encontró la compra solicitada."
            });
        }

        if (compra.ComprasDetalles.Count == 0)
        {
            return Conflict(new
            {
                mensaje = "La compra no tiene productos registrados."
            });
        }

        byte[] pdf = CrearPdfCompra(compra);

        string nombreArchivo =
            $"Comprobante_Compra_{compra.IdCompra:000000}.pdf";

        Response.Headers["Cache-Control"] = "no-store, private";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (descargar)
        {
            return File(pdf, "application/pdf", nombreArchivo);
        }

        Response.Headers["Content-Disposition"] =
            $"inline; filename=\"{nombreArchivo}\"";

        return File(pdf, "application/pdf");
    }

    private static byte[] CrearPdfCompra(Compra compra)
    {
        bool anulada = string.Equals(
            compra.Estado,
            "ANULADA",
            StringComparison.OrdinalIgnoreCase);

        var proveedor = compra.IdProveedorNavigation;

        string nombreProveedor = proveedor.Nombre;

        string nit = string.IsNullOrWhiteSpace(proveedor.Nit)
            ? "No registrado"
            : proveedor.Nit;

        string numeroFactura =
            string.IsNullOrWhiteSpace(compra.NumeroFactura)
                ? "No registrado"
                : compra.NumeroFactura;

        string fechaFactura =
            compra.FechaFactura?.ToString("dd/MM/yyyy")
                ?? "No registrada";

        var detalles = compra.ComprasDetalles
            .OrderBy(d => d.IdDetalleCompra)
            .ToList();

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(estilo =>
                    estilo.FontSize(10));

                // ENCABEZADO
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
                                    .Text("COMPROBANTE DE COMPRA")
                                    .FontSize(11)
                                    .FontColor(Colors.White);
                            });

                            row.ConstantItem(155).Column(derecha =>
                            {
                                derecha.Item()
                                    .AlignRight()
                                    .Text($"No. {compra.IdCompra:000000}")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                derecha.Item()
                                    .AlignRight()
                                    .Text(compra.FechaCompra.ToString(
                                        "dd/MM/yyyy HH:mm"))
                                    .FontSize(9)
                                    .FontColor(Colors.White);
                            });
                        });

                    col.Item()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Text("COMPROBANTE DE PAGO DE COMPRA")
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.Red.Darken2);
                });

                // CONTENIDO
                page.Content()
                    .PaddingVertical(18)
                    .Column(col =>
                    {
                        col.Spacing(16);

                        if (anulada)
                        {
                            col.Item()
                                .Background(Colors.Red.Lighten4)
                                .Padding(12)
                                .AlignCenter()
                                .Text("COMPRA ANULADA")
                                .FontSize(17)
                                .Bold()
                                .FontColor(Colors.Red.Darken3);
                        }

                        // PROVEEDOR Y FACTURA
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(datos =>
                            {
                                datos.Spacing(5);

                                datos.Item()
                                    .Text("DATOS DEL PROVEEDOR")
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                datos.Item()
                                    .Text($"Nombre: {nombreProveedor}");

                                datos.Item()
                                    .Text($"NIT: {nit}");

                                if (!string.IsNullOrWhiteSpace(
                                    proveedor.Direccion))
                                {
                                    datos.Item().Text(
                                        $"Dirección: {proveedor.Direccion}");
                                }
                            });

                            row.RelativeItem().Column(datos =>
                            {
                                datos.Spacing(5);

                                datos.Item()
                                    .Text("INFORMACIÓN DE LA COMPRA")
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                datos.Item()
                                    .Text($"Factura: {numeroFactura}");

                                datos.Item()
                                    .Text($"Fecha de factura: {fechaFactura}");

                                datos.Item()
                                    .Text("Fecha de compra: " +
                                        compra.FechaCompra.ToString(
                                            "dd/MM/yyyy HH:mm"));

                                datos.Item()
                                    .Text($"Estado: {compra.Estado}");
                            });
                        });

                        // PRODUCTOS
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
                                    .Text("Costo");

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
                                    .Text(Q(detalle.PrecioCosto));

                                tabla.Cell()
                                    .Element(EstiloDetalle)
                                    .AlignRight()
                                    .Text(Q(detalle.Subtotal));
                            }
                        });

                        // TOTAL
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
                                    .Text(Q(compra.Total))
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);
                            });

                        // OBSERVACIONES
                        if (!string.IsNullOrWhiteSpace(
                            compra.Observaciones))
                        {
                            col.Item().Column(datos =>
                            {
                                datos.Spacing(5);

                                datos.Item()
                                    .Text("OBSERVACIONES")
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                datos.Item()
                                    .Text(compra.Observaciones);
                            });
                        }

                        col.Item()
                            .PaddingTop(15)
                            .AlignCenter()
                            .Text("Comprobante interno de compra")
                            .Bold()
                            .FontSize(12);

                        col.Item()
                            .AlignCenter()
                            .Text(
                                "Este documento es un respaldo interno de " +
                                "la compra registrada en el sistema. " +
                                "La factura original corresponde al proveedor.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    });

                // PIE DE PÁGINA
                page.Footer()
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingTop(9)
                    .AlignCenter()
                    .Text(texto =>
                    {
                        texto.Span(
                            "Sistema Punto de Venta | Documento interno | Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
            });
        });

        return documento.GeneratePdf();
    }

    private static IContainer EstiloEncabezado(IContainer container)
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

    private static IContainer EstiloDetalle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(10)
            .PaddingHorizontal(6);
    }

    private static string Q(decimal cantidad)
    {
        return "Q " + cantidad.ToString("N2", Cultura);
    }
}
