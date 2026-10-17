using ApiVentas.Data;
using ApiVentas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public VentasController(ApplicationDbContext db) => _db = db;

        private BadRequestObjectResult Error(string mensaje) => BadRequest(new { mensaje });

        // Opciones para el formulario de venta: clientes, empleados y productos (solo lectura).
        [HttpGet("catalogos")]
        public async Task<IActionResult> Catalogos([FromQuery] bool soloActivos = false)
        {
            var clientes = await _db.Clientes.AsNoTracking()
                .Where(c => !soloActivos || c.estado)
                .OrderBy(c => c.nombre).ThenBy(c => c.apellido)
                .Select(c => new { c.id_cliente, c.nombre, c.apellido, c.nit })
                .ToListAsync();

            var empleados = await _db.EmpleadosRef.AsNoTracking()
                .Where(e => !soloActivos || e.estado)
                .OrderBy(e => e.nombre).ThenBy(e => e.apellido)
                .Select(e => new { e.id_empleado, e.nombre, e.apellido })
                .ToListAsync();

            var productos = await _db.ProductosRef.AsNoTracking()
                .Where(p => !soloActivos || p.estado)
                .OrderBy(p => p.descripcion)
                .Select(p => new { p.id_producto, p.descripcion, p.precio_venta, p.existencia })
                .ToListAsync();

            return Ok(new { clientes, empleados, productos });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Venta>>> Get()
            => await _db.Ventas.Include(v => v.Detalles)
                               .OrderByDescending(v => v.id_venta)
                               .ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Venta>> Get(int id)
        {
            var venta = await _db.Ventas.Include(v => v.Detalles)
                                        .FirstOrDefaultAsync(v => v.id_venta == id);
            return venta == null ? NotFound(new { mensaje = "No existe la venta." }) : venta;
        }

        // Confirma la venta: valida inventario, toma el precio de Productos,
        // calcula subtotales y total, y descuenta la existencia. Todo en una transacción:
        // si algo falla, no se guarda nada y el inventario queda intacto.
        [HttpPost]
        public async Task<IActionResult> Post(Venta venta)
        {
            if (venta.Detalles == null || venta.Detalles.Count == 0)
                return Error("La venta debe tener al menos un producto.");

            if (venta.Detalles.Any(d => d.cantidad <= 0))
                return Error("La cantidad de cada producto debe ser mayor a 0.");

            if (!await _db.Clientes.AnyAsync(c => c.id_cliente == venta.id_cliente && c.estado))
                return Error("El cliente no existe o está inactivo.");

            if (!await _db.EmpleadosRef.AnyAsync(e => e.id_empleado == venta.id_empleado && e.estado))
                return Error("El empleado no existe o está inactivo.");

            // Si un mismo producto viene en varias líneas, se suman sus cantidades.
            var pedidos = venta.Detalles
                .GroupBy(d => d.id_producto)
                .Select(g => new { Id = g.Key, Cantidad = g.Sum(x => x.cantidad) })
                .ToList();

            var ids = pedidos.Select(p => p.Id).ToList();
            var productos = await _db.ProductosRef.AsNoTracking()
                .Where(p => ids.Contains(p.id_producto))
                .ToDictionaryAsync(p => p.id_producto);

            // 1) Validar existencia de productos e inventario suficiente
            foreach (var p in pedidos)
            {
                if (!productos.TryGetValue(p.Id, out var prod) || !prod.estado)
                    return Error($"El producto {p.Id} no existe o está inactivo.");

                if (prod.existencia < p.Cantidad)
                    return Error($"Inventario insuficiente para '{prod.descripcion}': disponible {prod.existencia}, solicitado {p.Cantidad}.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 2) Descontar existencia. La condición existencia >= cantidad vuelve a validar
                //    en la base de datos, por si otra venta tomó el inventario mientras tanto.
                foreach (var p in pedidos)
                {
                    var id = p.Id;
                    var cantidad = p.Cantidad;

                    var filas = await _db.ProductosRef
                        .Where(x => x.id_producto == id && x.existencia >= cantidad)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.existencia, x => x.existencia - cantidad));

                    if (filas == 0)
                    {
                        await tx.RollbackAsync();
                        return Error($"Inventario insuficiente para '{productos[id].descripcion}'. Intenta de nuevo.");
                    }
                }

                // 3) Precio desde Productos, subtotal por línea y total de la venta
                foreach (var d in venta.Detalles)
                {
                    d.id_detalle_venta = 0;
                    d.precio_venta = productos[d.id_producto].precio_venta;
                    d.subtotal = d.cantidad * d.precio_venta;
                }

                venta.id_venta = 0;
                venta.fecha_venta = DateTime.Now;
                venta.estado = "REGISTRADA";
                venta.total = venta.Detalles.Sum(d => d.subtotal);

                _db.Ventas.Add(venta);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return CreatedAtAction(nameof(Get), new { id = venta.id_venta }, venta);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Solo se pueden cambiar cliente, empleado y observaciones.
        // Los productos no se tocan para no alterar el inventario.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, Venta datos)
        {
            var venta = await _db.Ventas.FindAsync(id);
            if (venta == null) return NotFound(new { mensaje = "No existe la venta." });

            if (!await _db.Clientes.AnyAsync(c => c.id_cliente == datos.id_cliente))
                return Error("El cliente no existe.");

            if (!await _db.EmpleadosRef.AnyAsync(e => e.id_empleado == datos.id_empleado))
                return Error("El empleado no existe.");

            venta.id_cliente = datos.id_cliente;
            venta.id_empleado = datos.id_empleado;
            venta.observaciones = datos.observaciones;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Al eliminar una venta registrada, la existencia vuelve al inventario.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var venta = await _db.Ventas.Include(v => v.Detalles)
                                        .FirstOrDefaultAsync(v => v.id_venta == id);
            if (venta == null) return NotFound(new { mensaje = "No existe la venta." });

            // Una venta con factura emitida no se puede eliminar.
            var fel = await _db.Facturas.Where(f => f.id_venta == id)
                                        .Select(f => (int?)f.id_factura)
                                        .FirstOrDefaultAsync();
            if (fel != null)
                return Conflict(new { mensaje = $"No se puede eliminar la venta porque ya tiene emitida la factura FEL No. {fel.Value:D6}." });

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (venta.estado == "REGISTRADA" && venta.Detalles != null)
                {
                    var devoluciones = venta.Detalles
                        .GroupBy(d => d.id_producto)
                        .Select(g => new { Id = g.Key, Cantidad = g.Sum(x => x.cantidad) });

                    foreach (var dev in devoluciones)
                    {
                        var idProd = dev.Id;
                        var cantidad = dev.Cantidad;
                        await _db.ProductosRef
                            .Where(x => x.id_producto == idProd)
                            .ExecuteUpdateAsync(s => s.SetProperty(x => x.existencia, x => x.existencia + cantidad));
                    }
                }

                if (venta.Detalles != null) _db.VentasDetalle.RemoveRange(venta.Detalles);
                _db.Ventas.Remove(venta);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return NoContent();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}