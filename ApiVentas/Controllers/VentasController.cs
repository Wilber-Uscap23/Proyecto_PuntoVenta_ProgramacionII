using ApiVentas.Data;
using ApiVentas.Helpers;
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
        private readonly IConfiguration _config;

        public VentasController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private BadRequestObjectResult Error(string mensaje) => BadRequest(new { mensaje });

        // Resultado de validar una venta (sirve para la vista previa y para confirmar).
        private sealed class Preparacion
        {
            public List<(int Id, int Cantidad)> Pedidos { get; init; } = new();
            public Dictionary<int, ProductoRef> Productos { get; init; } = new();
            public Cliente Cliente { get; init; } = null!;
            public decimal Total { get; init; }
        }

        // Valida todo lo que debe cumplir una venta: productos, inventario, cliente,
        // empleado y la regla de consumidor final para ventas mayores a Q 2,500.
        private async Task<(Preparacion? Datos, IActionResult? Fallo)> Validar(Venta venta)
        {
            if (venta.Detalles == null || venta.Detalles.Count == 0)
                return (null, Error("La venta debe tener al menos un producto."));

            if (venta.Detalles.Any(d => d.cantidad <= 0))
                return (null, Error("La cantidad de cada producto debe ser mayor a 0."));

            var cliente = await _db.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.id_cliente == venta.id_cliente && c.estado);
            if (cliente == null)
                return (null, Error("El cliente no existe o está inactivo."));

            if (!await _db.EmpleadosRef.AnyAsync(e => e.id_empleado == venta.id_empleado && e.estado))
                return (null, Error("El empleado no existe o está inactivo."));

            // Si un mismo producto viene en varias líneas, se suman sus cantidades.
            var pedidos = venta.Detalles
                .GroupBy(d => d.id_producto)
                .Select(g => (Id: g.Key, Cantidad: g.Sum(x => x.cantidad)))
                .ToList();

            var ids = pedidos.Select(p => p.Id).ToList();
            var productos = await _db.ProductosRef.AsNoTracking()
                .Where(p => ids.Contains(p.id_producto))
                .ToDictionaryAsync(p => p.id_producto);

            foreach (var p in pedidos)
            {
                if (!productos.TryGetValue(p.Id, out var prod) || !prod.estado)
                    return (null, Error($"El producto {p.Id} no existe o está inactivo."));

                if (prod.existencia < p.Cantidad)
                    return (null, Error($"Inventario insuficiente para '{prod.descripcion}': disponible {prod.existencia}, solicitado {p.Cantidad}."));
            }

            // El precio siempre se toma de Productos.
            var total = venta.Detalles.Sum(d => d.cantidad * productos[d.id_producto].precio_venta);

            var regla = ReglasFactura.Validar(total, cliente);
            if (regla != null)
                return (null, Error(regla));

            return (new Preparacion { Pedidos = pedidos, Productos = productos, Cliente = cliente, Total = total }, null);
        }

        // Busca un cliente activo por NIT. Si el NIT viene vacío se busca "CF" (consumidor final).
        [HttpGet("buscar-cliente")]
        public async Task<IActionResult> BuscarCliente([FromQuery] string? nit)
        {
            var t = (nit ?? "").Replace(" ", "").Replace("-", "").ToUpperInvariant();
            if (t == "") t = "CF";

            var plano = t;                                   // NIT sin guion
            if (t != "CF" && t.Length > 1) t = t[..^1] + "-" + t[^1];   // NIT con guion (como se guarda)

            var cliente = await _db.Clientes.AsNoTracking()
                .Where(c => c.estado && (c.nit == t || c.nit == plano))
                .Select(c => new { c.id_cliente, c.nombre, c.apellido, c.nit })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new
                {
                    mensaje = t == "CF"
                        ? "No hay un cliente activo con NIT CF. Regístralo en el módulo de Clientes."
                        : $"No se encontró un cliente activo con el NIT {t}."
                });
            }

            return Ok(cliente);
        }

        // Busca empleados activos por id o por parte del nombre.
        [HttpGet("buscar-empleados")]
        public async Task<IActionResult> BuscarEmpleados([FromQuery] string? texto)
        {
            var t = (texto ?? "").Trim();
            if (t == "") return Ok(new List<object>());

            var esId = int.TryParse(t, out var idNum);

            var lista = await _db.EmpleadosRef.AsNoTracking()
                .Where(e => e.estado && ((esId && e.id_empleado == idNum) || (e.nombre + " " + e.apellido).Contains(t)))
                .OrderBy(e => e.nombre).ThenBy(e => e.apellido)
                .Take(10)
                .Select(e => new { e.id_empleado, e.nombre, e.apellido })
                .ToListAsync();

            return Ok(lista);
        }

        // Busca un producto activo por código de barras / código, o por id.
        [HttpGet("buscar-producto")]
        public async Task<IActionResult> BuscarProducto([FromQuery] string? codigo)
        {
            var t = (codigo ?? "").Trim();
            if (t == "") return Error("Escribe o escanea un código.");

            var producto = await _db.ProductosRef.AsNoTracking()
                .Where(p => p.estado && p.codigo == t)
                .Select(p => new { p.id_producto, p.codigo, p.descripcion, p.precio_venta, p.existencia })
                .FirstOrDefaultAsync();

            if (producto == null && int.TryParse(t, out var idNum))
            {
                producto = await _db.ProductosRef.AsNoTracking()
                    .Where(p => p.estado && p.id_producto == idNum)
                    .Select(p => new { p.id_producto, p.codigo, p.descripcion, p.precio_venta, p.existencia })
                    .FirstOrDefaultAsync();
            }

            if (producto == null)
                return NotFound(new { mensaje = $"No se encontró un producto activo con el código '{t}'." });

            return Ok(producto);
        }

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
                .Select(p => new { p.id_producto, p.codigo, p.descripcion, p.precio_venta, p.existencia })
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

        // Vista previa de la factura: hace las mismas validaciones que al confirmar,
        // pero NO guarda nada ni descuenta inventario.
        [HttpPost("vista-previa")]
        public async Task<IActionResult> VistaPrevia(Venta venta)
        {
            if (!string.IsNullOrWhiteSpace(venta.observaciones))
                return Error("No se permiten comentarios en la venta.");

            var (datos, fallo) = await Validar(venta);
            if (fallo != null) return fallo;
            var prep = datos!;

            var lineas = venta.Detalles!
                .Select(d =>
                {
                    var prod = prep.Productos[d.id_producto];
                    return new FacturaLinea
                    {
                        descripcion = prod.descripcion,
                        cantidad = d.cantidad,
                        precio_unitario = prod.precio_venta,
                        subtotal = d.cantidad * prod.precio_venta
                    };
                })
                .ToList();

            var documento = FacturaBuilder.Construir(
                _config,
                0,                                   // el número FEL se asigna al confirmar
                DateTime.Now,
                ReglasFactura.NitComprador(prep.Cliente),
                $"{prep.Cliente.nombre} {prep.Cliente.apellido}",
                0,                                   // el número de venta se asigna al confirmar
                lineas,
                prep.Total);

            return Ok(documento);
        }

        // Confirma la venta: valida, descuenta la existencia, guarda la venta con sus detalles
        // y emite la factura (número FEL). Todo en una transacción: si algo falla, no se guarda nada.
        [HttpPost]
        public async Task<IActionResult> Post(Venta venta)
        {
            if (!string.IsNullOrWhiteSpace(venta.observaciones))
                return Error("No se permiten comentarios en la venta.");

            var (datos, fallo) = await Validar(venta);
            if (fallo != null) return fallo;
            var prep = datos!;
            var detalles = venta.Detalles!;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Descontar existencia. La condición existencia >= cantidad vuelve a validar
                // en la base de datos, por si otra venta tomó el inventario mientras tanto.
                foreach (var pedido in prep.Pedidos)
                {
                    var id = pedido.Id;
                    var cantidad = pedido.Cantidad;

                    var filas = await _db.ProductosRef
                        .Where(x => x.id_producto == id && x.existencia >= cantidad)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.existencia, x => x.existencia - cantidad));

                    if (filas == 0)
                    {
                        await tx.RollbackAsync();
                        return Error($"Inventario insuficiente para '{prep.Productos[id].descripcion}'. Intenta de nuevo.");
                    }
                }

                foreach (var d in detalles)
                {
                    d.id_detalle_venta = 0;
                    d.precio_venta = prep.Productos[d.id_producto].precio_venta;
                    d.subtotal = d.cantidad * d.precio_venta;
                }

                venta.id_venta = 0;
                venta.fecha_venta = DateTime.Now;
                venta.estado = "REGISTRADA";
                venta.total = prep.Total;
                venta.observaciones = null;

                _db.Ventas.Add(venta);
                await _db.SaveChangesAsync();

                // La factura se guarda junto con la venta (aquí se asigna el número FEL).
                _db.Facturas.Add(new Factura
                {
                    id_venta = venta.id_venta,
                    fecha_emision = venta.fecha_venta,
                    nit_comprador = ReglasFactura.NitComprador(prep.Cliente),
                    nombre_comprador = $"{prep.Cliente.nombre} {prep.Cliente.apellido}",
                    total = venta.total
                });
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

        // Solo se pueden cambiar cliente y empleado. Los productos y los comentarios no se tocan.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, Venta datos)
        {
            var venta = await _db.Ventas.FindAsync(id);
            if (venta == null) return NotFound(new { mensaje = "No existe la venta." });

            var cliente = await _db.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.id_cliente == datos.id_cliente);
            if (cliente == null)
                return Error("El cliente no existe.");

            if (!await _db.EmpleadosRef.AnyAsync(e => e.id_empleado == datos.id_empleado))
                return Error("El empleado no existe.");

            var regla = ReglasFactura.Validar(venta.total, cliente);
            if (regla != null)
                return Error(regla);

            if (datos.id_cliente != venta.id_cliente &&
                await _db.Facturas.AnyAsync(f => f.id_venta == id))
            {
                return Conflict(new { mensaje = "La venta ya tiene factura emitida, no se puede cambiar el cliente." });
            }

            venta.id_cliente = datos.id_cliente;
            venta.id_empleado = datos.id_empleado;

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