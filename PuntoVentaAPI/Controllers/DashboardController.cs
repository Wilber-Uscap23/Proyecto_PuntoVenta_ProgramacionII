using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;

namespace PuntoVentaAPI.Controllers;

// Los indicadores globales del negocio se entregan únicamente a ADMIN.
// Las rutas existentes de Ventas, Compras y Productos conservan sus permisos actuales.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class DashboardController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public DashboardController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerDashboard()
    {
        // Mantenemos los mismos campos que esperan los modelos de Home.razor.
        // No hay escrituras, cambios de inventario ni modificaciones a MySQL.
        var ventas = await _context.Ventas
            .AsNoTracking()
            .OrderByDescending(v => v.IdVenta)
            .Select(v => new
            {
                v.IdVenta,
                NombreCliente = v.IdClienteNavigation.Nombre + " " +
                                v.IdClienteNavigation.Apellido,
                v.FechaVenta,
                v.Total,
                v.Estado
            })
            .ToListAsync();

        var compras = await _context.Compras
            .AsNoTracking()
            .Select(c => new
            {
                c.IdCompra,
                c.FechaCompra,
                c.Total,
                c.Estado
            })
            .ToListAsync();

        var productos = await _context.Productos
            .AsNoTracking()
            .Select(p => new
            {
                p.IdProducto,
                p.Existencia,
                p.Estado
            })
            .ToListAsync();

        return Ok(new { ventas, compras, productos });
    }
}
