using ComprasApp.Api.Data;
using ComprasApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComprasApp.Api.Controllers;

// STUB de solo lectura para poblar el combo de productos en Compras.
// Reemplazar/fusionar con el ProductosController real de Wilber.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Producto>>> Listar() =>
        await db.Productos.AsNoTracking().OrderBy(p => p.Descripcion).ToListAsync();
}
