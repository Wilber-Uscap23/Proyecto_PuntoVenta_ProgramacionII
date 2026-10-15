using ComprasApp.Api.Data;
using ComprasApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComprasApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProveedoresController(AppDbContext db) : ControllerBase
{
    // GET api/proveedores?buscar=texto
    [HttpGet]
    public async Task<ActionResult<List<Proveedor>>> Listar([FromQuery] string? buscar)
    {
        var q = db.Proveedores.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            buscar = buscar.Trim();
            q = q.Where(p => p.Nombre.Contains(buscar) || p.Nit.Contains(buscar)
                          || p.Correo.Contains(buscar) || p.Telefono.Contains(buscar));
        }
        return await q.OrderBy(p => p.Nombre).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Proveedor>> Obtener(int id)
    {
        var p = await db.Proveedores.AsNoTracking().FirstOrDefaultAsync(x => x.IdProveedor == id);
        return p is null ? NotFound(new { message = "Proveedor no encontrado." }) : p;
    }

    [HttpPost]
    public async Task<ActionResult<Proveedor>> Crear(Proveedor modelo)
    {
        Normalizar(modelo);
        if (await db.Proveedores.AnyAsync(p => p.Nit == modelo.Nit))
            return Conflict(new { message = "Ya existe un proveedor con ese NIT." });

        modelo.IdProveedor = 0;
        db.Proveedores.Add(modelo);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = modelo.IdProveedor }, modelo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, Proveedor modelo)
    {
        if (id != modelo.IdProveedor) return BadRequest(new { message = "El id no coincide." });
        Normalizar(modelo);

        var existente = await db.Proveedores.FindAsync(id);
        if (existente is null) return NotFound(new { message = "Proveedor no encontrado." });

        if (await db.Proveedores.AnyAsync(p => p.Nit == modelo.Nit && p.IdProveedor != id))
            return Conflict(new { message = "Ya existe otro proveedor con ese NIT." });

        existente.Nombre = modelo.Nombre;
        existente.Nit = modelo.Nit;
        existente.Telefono = modelo.Telefono;
        existente.Direccion = modelo.Direccion;
        existente.Correo = modelo.Correo;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var p = await db.Proveedores.FindAsync(id);
        if (p is null) return NotFound(new { message = "Proveedor no encontrado." });

        if (await db.Compras.AnyAsync(c => c.IdProveedor == id))
            return Conflict(new { message = "No se puede eliminar: el proveedor tiene compras registradas." });

        db.Proveedores.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void Normalizar(Proveedor p)
    {
        p.Nombre = p.Nombre.Trim();
        p.Nit = p.Nit.Trim().ToUpperInvariant();
        p.Telefono = p.Telefono.Trim();
        p.Direccion = p.Direccion.Trim();
        p.Correo = p.Correo.Trim().ToLowerInvariant();
    }
}
