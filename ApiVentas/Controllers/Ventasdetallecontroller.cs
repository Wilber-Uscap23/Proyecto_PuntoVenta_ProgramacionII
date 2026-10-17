using ApiVentas.Data;
using ApiVentas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiVentas.Controllers
{
    // Solo lectura. Los detalles se crean, y el inventario se descuenta,
    // únicamente al confirmar la venta (POST api/ventas).
    [ApiController]
    [Route("api/[controller]")]
    public class VentasDetalleController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public VentasDetalleController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VentasDetalle>>> Get([FromQuery] int? idVenta)
        {
            var q = _db.VentasDetalle.AsNoTracking().AsQueryable();
            if (idVenta != null) q = q.Where(d => d.id_venta == idVenta);
            return await q.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VentasDetalle>> Get(int id)
        {
            var d = await _db.VentasDetalle.AsNoTracking().FirstOrDefaultAsync(x => x.id_detalle_venta == id);
            return d == null ? NotFound(new { mensaje = "No existe el detalle." }) : d;
        }
    }
}