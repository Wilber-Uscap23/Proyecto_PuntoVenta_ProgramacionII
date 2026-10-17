using ApiVentas.Data;
using ApiVentas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiVentas.Controllers
{
    // Búsquedas puntuales para la pantalla de venta.
    // Nunca devuelven tablas completas: 1 cliente por NIT, 1 producto por código, máximo 10 empleados.
    [ApiController]
    [Route("api/busqueda")]
    public class BusquedaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public BusquedaController(ApplicationDbContext db) => _db = db;

        // GET api/busqueda/cliente/3602978-5     (también acepta CF)
        [HttpGet("cliente/{nit}")]
        public async Task<IActionResult> ClientePorNit(string nit)
        {
            // Reutiliza la validación de NIT (módulo 11) que ya tiene Models/cliente.cs
            if (string.IsNullOrWhiteSpace(nit) || !NitValidoAttribute.EsValido(nit))
                return BadRequest(new { mensaje = "NIT inválido. Escribe CF o un NIT con su dígito verificador correcto (ej. 3602978-5)." });

            // En la tabla el NIT se guarda como "3602978-5" (o "CF"), igual que hace Cliente.Normalizar()
            var nitFormateado = Formatear(nit);

            var c = await _db.Clientes
                .AsNoTracking()
                .Where(x => x.nit == nitFormateado)   // nit es único: usa el índice, rápido con millones de filas
                .Select(x => new { x.id_cliente, x.nombre, x.apellido, x.nit, x.estado })
                .FirstOrDefaultAsync();

            if (c == null)
                return NotFound(new { mensaje = "No existe un cliente con ese NIT." });

            if (!c.estado)
                return Conflict(new { mensaje = "El cliente con ese NIT está inactivo. Actívalo en el módulo de Clientes para venderle." });

            return Ok(new { c.id_cliente, c.nombre, c.apellido, c.nit });
        }

        // GET api/busqueda/empleados?q=5                  (por id)
        // GET api/busqueda/empleados?q=maria              (por nombre o apellido)
        // GET api/busqueda/empleados?q=5&soloActivos=false (para mostrar el empleado de una venta ya guardada)
        [HttpGet("empleados")]
        public async Task<IActionResult> Empleados([FromQuery] string? q, [FromQuery] bool soloActivos = true)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { mensaje = "Escribe el id o el nombre del empleado." });

            q = q.Trim();
            IQueryable<EmpleadoRef> query = _db.EmpleadosRef.AsNoTracking()
                .Where(e => !soloActivos || e.estado);

            if (int.TryParse(q, out var id))
            {
                query = query.Where(e => e.id_empleado == id);
            }
            else
            {
                if (q.Length < 2)
                    return BadRequest(new { mensaje = "Escribe al menos 2 letras del nombre." });

                query = query.Where(e => (e.nombre + " " + e.apellido).Contains(q));
            }

            var lista = await query
                .OrderBy(e => e.nombre).ThenBy(e => e.apellido)
                .Take(10)
                .Select(e => new { e.id_empleado, e.nombre, e.apellido })
                .ToListAsync();

            return Ok(lista);
        }

        // GET api/busqueda/producto?codigo=7501055300075
        // Primero busca por código de barras (columna "codigo", única). Si no existe y el texto
        // es un número, busca por id_producto.
        [HttpGet("producto")]
        public async Task<IActionResult> ProductoPorCodigo([FromQuery] string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { mensaje = "Escribe o escanea el código del producto." });

            codigo = codigo.Trim();
            if (codigo.Length > 50)
                return BadRequest(new { mensaje = "El código no puede pasar de 50 caracteres." });

            var q = _db.ProductosRef.AsNoTracking();

            var p = await q.Where(x => x.codigo == codigo)
                .Select(x => new { x.id_producto, x.codigo, x.descripcion, x.precio_venta, x.existencia, x.estado })
                .FirstOrDefaultAsync();

            if (p == null && int.TryParse(codigo, out var id))
            {
                p = await q.Where(x => x.id_producto == id)
                    .Select(x => new { x.id_producto, x.codigo, x.descripcion, x.precio_venta, x.existencia, x.estado })
                    .FirstOrDefaultAsync();
            }

            if (p == null)
                return NotFound(new { mensaje = "No existe un producto con ese código o id." });

            if (!p.estado)
                return Conflict(new { mensaje = $"El producto \"{p.descripcion}\" está inactivo y no se puede vender." });

            return Ok(new { p.id_producto, p.codigo, p.descripcion, p.precio_venta, p.existencia });
        }

        // GET api/busqueda/productos?ids=1,2,3
        // Solo para mostrar los productos de una venta ya guardada (incluye inactivos).
        [HttpGet("productos")]
        public async Task<IActionResult> ProductosPorIds([FromQuery] string? ids)
        {
            var lista = (ids ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                .Where(n => n != null)
                .Select(n => n!.Value)
                .Distinct()
                .Take(200)
                .ToList();

            if (lista.Count == 0)
                return BadRequest(new { mensaje = "Indica al menos un id de producto." });

            var productos = await _db.ProductosRef.AsNoTracking()
                .Where(p => lista.Contains(p.id_producto))
                .Select(p => new { p.id_producto, p.codigo, p.descripcion, p.precio_venta, p.existencia })
                .ToListAsync();

            return Ok(productos);
        }

        private static string Formatear(string nit)
        {
            var t = nit.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();
            if (t == "CF" || t.Length < 2) return t;
            return t[..^1] + "-" + t[^1];
        }
    }
}