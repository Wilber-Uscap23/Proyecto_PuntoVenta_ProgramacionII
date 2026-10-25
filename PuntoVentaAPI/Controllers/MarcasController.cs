
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace PuntoVentaAPI.Controllers;

// DTO PARA CREAR Y ACTUALIZAR MARCAS
public class CrearMarcaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripción no puede superar 255 caracteres.")]
    public string? Descripcion { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class MarcasController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public MarcasController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ============================================
    // GET: CONSULTAR TODAS LAS MARCAS
    // ============================================
    [HttpGet]
    public async Task<IActionResult> ObtenerMarcas()
    {
        var marcas = await _context.Marcas
            .AsNoTracking()
            .OrderBy(m => m.IdMarca)
            .Select(m => new
            {
                m.IdMarca,
                m.Nombre,
                m.Descripcion,
                m.Estado
            })
            .ToListAsync();

        return Ok(marcas);
    }

    // ============================================
    // GET: CONSULTAR MARCA POR ID
    // ============================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerMarcaPorId(int id)
    {
        var marca = await _context.Marcas
            .AsNoTracking()
            .Where(m => m.IdMarca == id)
            .Select(m => new
            {
                m.IdMarca,
                m.Nombre,
                m.Descripcion,
                m.Estado
            })
            .FirstOrDefaultAsync();

        if (marca == null)
        {
            return NotFound(new
            {
                mensaje = "La marca no existe."
            });
        }

        return Ok(marca);
    }

    // ============================================
    // POST: REGISTRAR NUEVA MARCA
    // ============================================
    [HttpPost]
    public async Task<IActionResult> RegistrarMarca(
        [FromBody] CrearMarcaDto datos)
    {
        string nombre = datos.Nombre?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre de la marca es obligatorio."
            });
        }

        if (nombre.Length > 100)
        {
            return BadRequest(new
            {
                mensaje = "El nombre no debe superar 100 caracteres."
            });
        }

        string? descripcion = LimpiarDescripcion(datos.Descripcion);

        if (descripcion?.Length > 255)
        {
            return BadRequest(new
            {
                mensaje = "La descripción no debe superar 255 caracteres."
            });
        }

        bool nombreExiste = await _context.Marcas
            .AnyAsync(m => m.Nombre == nombre);

        if (nombreExiste)
        {
            return Conflict(new
            {
                mensaje = "Ya existe una marca con ese nombre."
            });
        }

        var marca = new Marca
        {
            Nombre = nombre,
            Descripcion = descripcion,
            Estado = true
        };

        _context.Marcas.Add(marca);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenerMarcaPorId),
            new { id = marca.IdMarca },
            new
            {
                mensaje = "Marca registrada correctamente.",
                marca.IdMarca,
                marca.Nombre,
                marca.Descripcion,
                marca.Estado
            });
    }

    // ============================================
    // PUT: ACTUALIZAR MARCA
    // ============================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarMarca(
        int id,
        [FromBody] CrearMarcaDto datos)
    {
        var marca = await _context.Marcas
            .FirstOrDefaultAsync(m => m.IdMarca == id);

        if (marca == null)
        {
            return NotFound(new
            {
                mensaje = "La marca que deseas editar no existe."
            });
        }

        string nombre = datos.Nombre?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre de la marca es obligatorio."
            });
        }

        if (nombre.Length > 100)
        {
            return BadRequest(new
            {
                mensaje = "El nombre no debe superar 100 caracteres."
            });
        }

        string? descripcion = LimpiarDescripcion(datos.Descripcion);

        if (descripcion?.Length > 255)
        {
            return BadRequest(new
            {
                mensaje = "La descripción no debe superar 255 caracteres."
            });
        }

        bool duplicada = await _context.Marcas.AnyAsync(m =>
            m.Nombre == nombre &&
            m.IdMarca != id);

        if (duplicada)
        {
            return Conflict(new
            {
                mensaje = "El nombre ya pertenece a otra marca."
            });
        }

        marca.Nombre = nombre;
        marca.Descripcion = descripcion;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Marca actualizada correctamente.",
            marca.IdMarca,
            marca.Nombre,
            marca.Descripcion,
            marca.Estado
        });
    }

    // ============================================
    // DELETE: DESACTIVAR MARCA
    // ============================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DesactivarMarca(int id)
    {
        var marca = await _context.Marcas
            .FirstOrDefaultAsync(m => m.IdMarca == id);

        if (marca == null)
        {
            return NotFound(new
            {
                mensaje = "La marca no existe."
            });
        }

        if (marca.Estado != true)
        {
            return BadRequest(new
            {
                mensaje = "La marca ya se encuentra inactiva."
            });
        }

        // NO DESACTIVAR SI TIENE PRODUCTOS ACTIVOS
        bool productosActivos = await _context.Productos
            .AnyAsync(p =>
                p.IdMarca == id &&
                p.Estado == true);

        if (productosActivos)
        {
            return Conflict(new
            {
                mensaje = "No se puede desactivar esta marca porque tiene productos activos asociados. Desactiva o reasigna primero esos productos."
            });
        }

        // DESACTIVACIÓN LÓGICA
        marca.Estado = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Marca desactivada correctamente.",
            marca.IdMarca,
            marca.Estado
        });
    }

    // ============================================
    // PUT: REACTIVAR MARCA
    // ============================================
    [HttpPut("{id:int}/activar")]
    public async Task<IActionResult> ActivarMarca(int id)
    {
        var marca = await _context.Marcas
            .FirstOrDefaultAsync(m => m.IdMarca == id);

        if (marca == null)
        {
            return NotFound(new
            {
                mensaje = "La marca no existe."
            });
        }

        if (marca.Estado == true)
        {
            return BadRequest(new
            {
                mensaje = "La marca ya se encuentra activa."
            });
        }

        marca.Estado = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Marca activada correctamente.",
            marca.IdMarca,
            marca.Estado
        });
    }

    // ============================================
    // LIMPIEZA DE DESCRIPCIONES
    // ============================================
    private static string? LimpiarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return null;

        return descripcion.Trim();
    }
}
