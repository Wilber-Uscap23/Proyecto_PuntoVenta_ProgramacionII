using ApiVentas.Data;
using ApiVentas.Helpers;
using ApiVentas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public FacturasController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // Emite la factura de una venta. Si la venta ya tiene factura, devuelve la misma
        // (con el mismo número FEL), así que se puede llamar las veces que sea necesario.
        [HttpPost("venta/{idVenta:int}")]
        public async Task<IActionResult> Emitir(int idVenta)
        {
            var venta = await _db.Ventas.AsNoTracking()
                                        .Include(v => v.Detalles)
                                        .FirstOrDefaultAsync(v => v.id_venta == idVenta);
            if (venta == null)
                return NotFound(new { mensaje = "No existe la venta." });

            if (venta.Detalles == null || venta.Detalles.Count == 0)
                return BadRequest(new { mensaje = "La venta no tiene productos, no se puede facturar." });

            var factura = await _db.Facturas.AsNoTracking()
                                            .FirstOrDefaultAsync(f => f.id_venta == idVenta);

            if (factura == null)
            {
                var cliente = await _db.Clientes.AsNoTracking()
                                                .FirstOrDefaultAsync(c => c.id_cliente == venta.id_cliente);
                if (cliente == null)
                    return BadRequest(new { mensaje = "El cliente de la venta ya no existe." });

                var nueva = new Factura
                {
                    id_venta = idVenta,
                    fecha_emision = DateTime.Now,
                    nit_comprador = string.IsNullOrWhiteSpace(cliente.nit) ? "CF" : cliente.nit,
                    nombre_comprador = $"{cliente.nombre} {cliente.apellido}",
                    total = venta.total
                };

                try
                {
                    _db.Facturas.Add(nueva);
                    await _db.SaveChangesAsync();
                    factura = nueva;
                }
                catch (DbUpdateException)
                {
                    // Si dos personas piden la factura al mismo tiempo, la segunda recupera la que ya se creó.
                    _db.ChangeTracker.Clear();
                    factura = await _db.Facturas.AsNoTracking()
                                                .FirstOrDefaultAsync(f => f.id_venta == idVenta);
                    if (factura == null)
                        return Conflict(new { mensaje = "No se pudo emitir la factura. Verifica que la tabla facturas exista en MySQL." });
                }
            }

            var ids = venta.Detalles.Select(d => d.id_producto).Distinct().ToList();
            var nombres = await _db.ProductosRef.AsNoTracking()
                                                .Where(p => ids.Contains(p.id_producto))
                                                .ToDictionaryAsync(p => p.id_producto, p => p.descripcion);

            var lineas = venta.Detalles
                .OrderBy(d => d.id_detalle_venta)
                .Select(d => new FacturaLinea
                {
                    descripcion = nombres.GetValueOrDefault(d.id_producto) ?? $"Producto {d.id_producto}",
                    cantidad = d.cantidad,
                    precio_unitario = d.precio_venta,
                    subtotal = d.subtotal
                })
                .ToList();

            var documento = new FacturaDocumento
            {
                numero_fel = factura.id_factura,
                serie = _config["Empresa:Serie"] ?? "A",
                fecha_emision = factura.fecha_emision,

                emisor_nombre = _config["Empresa:Nombre"] ?? "NOMBRE DE TU EMPRESA",
                emisor_nit = _config["Empresa:Nit"] ?? "NIT DE TU EMPRESA",
                emisor_direccion = _config["Empresa:Direccion"] ?? "DIRECCIÓN DE TU EMPRESA",

                comprador_nit = factura.nit_comprador,
                comprador_nombre = factura.nombre_comprador,

                id_venta = venta.id_venta,
                observaciones = venta.observaciones,

                Detalles = lineas,
                total_unidades = lineas.Sum(l => l.cantidad),
                descuento = 0m,
                total = factura.total,
                total_letras = NumeroLetras.Convertir(factura.total)
            };

            return Ok(documento);
        }
    }
}