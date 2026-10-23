using ApiProductos.Data;
using ApiProductos.Dtos;
using ApiProductos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiProductos.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MarcasController(AppDbContext db) : ControllerBase
{
    // GET api/marcas?buscar=sony
    [HttpGet]
    public async Task<ActionResult<List<MarcaDto>>> Listar([FromQuery] string? buscar)
    {
        var consulta = db.Marcas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(m => m.Nombre.Contains(texto)
                                        || (m.Descripcion != null && m.Descripcion.Contains(texto)));
        }

        return await consulta
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto
            {
                IdMarca = m.IdMarca,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                Estado = m.Estado
            })
            .ToListAsync();
    }

    // GET api/marcas/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MarcaDto>> Obtener(int id)
    {
        var m = await db.Marcas.AsNoTracking().FirstOrDefaultAsync(x => x.IdMarca == id);
        return m is null ? NotFound(new { mensaje = "La marca no existe." }) : Ok(ADto(m));
    }

    // POST api/marcas
    [HttpPost]
    public async Task<ActionResult<MarcaDto>> Crear(MarcaDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (await db.Marcas.AnyAsync(m => m.Nombre == nombre))
            return Conflict(new { mensaje = $"Ya existe una marca llamada '{nombre}'." });

        var marca = new Marca
        {
            Nombre = nombre,
            Descripcion = LimpiarTexto(dto.Descripcion),
            Estado = dto.Estado
        };
        db.Marcas.Add(marca);

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Conflict(new { mensaje = $"Ya existe una marca llamada '{nombre}'." });
        }

        return CreatedAtAction(nameof(Obtener), new { id = marca.IdMarca }, ADto(marca));
    }

    // PUT api/marcas/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MarcaDto>> Modificar(int id, MarcaDto dto)
    {
        var marca = await db.Marcas.FindAsync(id);
        if (marca is null)
            return NotFound(new { mensaje = "La marca no existe." });

        var nombre = dto.Nombre.Trim();
        if (await db.Marcas.AnyAsync(m => m.Nombre == nombre && m.IdMarca != id))
            return Conflict(new { mensaje = $"Ya existe otra marca llamada '{nombre}'." });

        marca.Nombre = nombre;
        marca.Descripcion = LimpiarTexto(dto.Descripcion);
        marca.Estado = dto.Estado;

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Conflict(new { mensaje = $"Ya existe otra marca llamada '{nombre}'." });
        }

        return Ok(ADto(marca));
    }

    // DELETE api/marcas/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var marca = await db.Marcas.FindAsync(id);
        if (marca is null)
            return NotFound(new { mensaje = "La marca no existe." });

        if (await db.Productos.AnyAsync(p => p.IdMarca == id))
            return Conflict(new { mensaje = "No se puede eliminar la marca porque tiene productos asociados." });

        db.Marcas.Remove(marca);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static MarcaDto ADto(Marca m) => new()
    {
        IdMarca = m.IdMarca,
        Nombre = m.Nombre,
        Descripcion = m.Descripcion,
        Estado = m.Estado
    };

    private static string? LimpiarTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
