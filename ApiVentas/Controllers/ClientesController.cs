using ApiVentas.Data;
using ApiVentas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ClientesController(ApplicationDbContext db) => _db = db;

        // GET api/clientes?buscar=juan&estado=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> Get([FromQuery] string? buscar, [FromQuery] bool? estado)
        {
            var q = _db.Clientes.AsNoTracking().AsQueryable();

            if (estado != null)
            {
                var activo = estado.Value;
                q = q.Where(c => c.estado == activo);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var t = buscar.Trim();
                q = q.Where(c => (c.nombre + " " + c.apellido).Contains(t)
                              || (c.nit ?? "").Contains(t)
                              || (c.telefono ?? "").Contains(t)
                              || (c.correo ?? "").Contains(t)
                              || (c.direccion ?? "").Contains(t));
            }

            return await q.OrderBy(c => c.nombre).ThenBy(c => c.apellido).ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Cliente>> Get(int id)
        {
            var c = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.id_cliente == id);
            return c == null ? NotFound(new { mensaje = "No existe el cliente." }) : c;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Cliente cliente)
        {
            cliente.Normalizar();

            var duplicado = await Duplicado(cliente, 0);
            if (duplicado != null) return Conflict(new { mensaje = duplicado });

            cliente.id_cliente = 0;
            cliente.fecha_registro = DateTime.Now;

            try
            {
                _db.Clientes.Add(cliente);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { mensaje = "No se pudo guardar el cliente. Verifica que el NIT y el correo no estén repetidos." });
            }

            return CreatedAtAction(nameof(Get), new { id = cliente.id_cliente }, cliente);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, Cliente datos)
        {
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null) return NotFound(new { mensaje = "No existe el cliente." });

            datos.Normalizar();

            var duplicado = await Duplicado(datos, id);
            if (duplicado != null) return Conflict(new { mensaje = duplicado });

            cliente.nombre = datos.nombre;
            cliente.apellido = datos.apellido;
            cliente.nit = datos.nit;
            cliente.telefono = datos.telefono;
            cliente.correo = datos.correo;
            cliente.direccion = datos.direccion;
            cliente.estado = datos.estado;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { mensaje = "No se pudo guardar el cliente. Verifica que el NIT y el correo no estén repetidos." });
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _db.Clientes.FindAsync(id);
            if (cliente == null) return NotFound(new { mensaje = "No existe el cliente." });

            if (await _db.Ventas.AnyAsync(v => v.id_cliente == id))
                return Conflict(new { mensaje = "No se puede eliminar el cliente porque tiene ventas registradas. Márcalo como inactivo en su lugar." });

            try
            {
                _db.Clientes.Remove(cliente);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { mensaje = "No se puede eliminar el cliente porque tiene registros asociados. Márcalo como inactivo en su lugar." });
            }

            return NoContent();
        }

        // En la tabla, nit y correo son únicos.
        private async Task<string?> Duplicado(Cliente c, int idActual)
        {
            if (c.nit != null && await _db.Clientes.AnyAsync(x => x.nit == c.nit && x.id_cliente != idActual))
            {
                return c.nit == "CF"
                    ? "Ya existe un cliente con NIT CF. Solo se permite uno; para otros clientes deja el NIT vacío."
                    : $"Ya existe un cliente con el NIT {c.nit}.";
            }

            if (c.correo != null && await _db.Clientes.AnyAsync(x => x.correo == c.correo && x.id_cliente != idActual))
                return $"Ya existe un cliente con el correo {c.correo}.";

            return null;
        }
    }
}