using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PuntoVentaAPI.Controllers;

// ============================================
// DATOS Y VALIDACIONES PARA GUARDAR PUESTOS
// ============================================

public class GuardarPuestoDto : IValidatableObject
{
    [Required(ErrorMessage = "El nombre del puesto es obligatorio.")]
    [StringLength(100, MinimumLength = 3,
        ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255,
        ErrorMessage = "La descripción no debe superar 255 caracteres.")]
    public string? Descripcion { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        string nombre = Nombre?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(nombre))
        {
            if (nombre.Length < 3 ||
                nombre.Length > 100)
            {
                yield return new ValidationResult(
                    "El nombre debe tener entre 3 y 100 caracteres.",
                    new[] { nameof(Nombre) });
            }
            else if (!Regex.IsMatch(
                nombre, @"^[\p{L}\p{M}\p{N} .,&()'/\-]+$"))
            {
                yield return new ValidationResult(
                    "El nombre contiene caracteres no permitidos.",
                    new[] { nameof(Nombre) });
            }
            else if (!nombre.Any(char.IsLetter))
            {
                yield return new ValidationResult(
                    "El nombre debe contener letras, no solamente números.",
                    new[] { nameof(Nombre) });
            }
        }

        if (!string.IsNullOrWhiteSpace(Descripcion))
        {
            string descripcion = Descripcion.Trim();

            if (!descripcion.Any(char.IsLetter))
            {
                yield return new ValidationResult(
                    "La descripción debe contener información descriptiva.",
                    new[] { nameof(Descripcion) });
            }
        }
    }
}

// ============================================
// CONTROLADOR DE PUESTOS
// ============================================

// Únicamente el rol ADMIN puede acceder
// a las operaciones de este controlador.

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class PuestosController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public PuestosController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // GET: LISTADO DE PUESTOS
    // ========================================

    [HttpGet]
    public async Task<IActionResult> ObtenerPuestos()
    {
        var puestos = await _context.Puestos
            .AsNoTracking()
            .OrderBy(p => p.IdPuesto)
            .Select(p => new
            {
                p.IdPuesto,
                p.Nombre,
                p.Descripcion,
                p.Estado
            })
            .ToListAsync();

        return Ok(puestos);
    }

    // ========================================
    // GET: CONSULTAR PUESTO POR ID
    // ========================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPuestoPorId(int id)
    {
        var puesto = await _context.Puestos
            .AsNoTracking()
            .Where(p => p.IdPuesto == id)
            .Select(p => new
            {
                p.IdPuesto,
                p.Nombre,
                p.Descripcion,
                p.Estado
            })
            .FirstOrDefaultAsync();

        if (puesto == null)
        {
            return NotFound(new
            {
                mensaje = "El puesto no existe."
            });
        }

        return Ok(puesto);
    }

    // ========================================
    // POST: REGISTRAR PUESTO
    // ========================================

    [HttpPost]
    public async Task<IActionResult> RegistrarPuesto(
        [FromBody] GuardarPuestoDto datos)
    {
        string nombre = NormalizarNombre(datos.Nombre);
        string? descripcion = Limpiar(datos.Descripcion);

        if (await _context.Puestos.AnyAsync(
            p => p.Nombre == nombre))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un puesto con ese nombre."
            });
        }

        var puesto = new Puesto
        {
            Nombre = nombre,
            Descripcion = descripcion,
            Estado = true
        };

        _context.Puestos.Add(puesto);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (EsDuplicado(ex))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un puesto con ese nombre."
            });
        }

        return CreatedAtAction(
            nameof(ObtenerPuestoPorId),
            new { id = puesto.IdPuesto },
            new
            {
                mensaje = "Puesto registrado correctamente.",
                puesto.IdPuesto,
                puesto.Nombre,
                puesto.Descripcion,
                puesto.Estado
            });
    }

    // ========================================
    // PUT: EDITAR PUESTO
    // ========================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarPuesto(
        int id,
        [FromBody] GuardarPuestoDto datos)
    {
        var puesto = await _context.Puestos
            .FirstOrDefaultAsync(p => p.IdPuesto == id);

        if (puesto == null)
        {
            return NotFound(new
            {
                mensaje = "El puesto que deseas editar no existe."
            });
        }

        string nombre = NormalizarNombre(datos.Nombre);
        string? descripcion = Limpiar(datos.Descripcion);

        bool duplicado = await _context.Puestos
            .AnyAsync(p =>
                p.Nombre == nombre &&
                p.IdPuesto != id);

        if (duplicado)
        {
            return Conflict(new
            {
                mensaje = "El nombre pertenece a otro puesto."
            });
        }

        puesto.Nombre = nombre;
        puesto.Descripcion = descripcion;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (EsDuplicado(ex))
        {
            return Conflict(new
            {
                mensaje = "El nombre pertenece a otro puesto."
            });
        }

        return Ok(new
        {
            mensaje = "Puesto actualizado correctamente.",
            puesto.IdPuesto,
            puesto.Nombre,
            puesto.Descripcion,
            puesto.Estado
        });
    }

    // ========================================
    // DELETE: DESACTIVAR PUESTO
    // ========================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DesactivarPuesto(int id)
    {
        var puesto = await _context.Puestos
            .FirstOrDefaultAsync(p => p.IdPuesto == id);

        if (puesto == null)
        {
            return NotFound(new
            {
                mensaje = "El puesto no existe."
            });
        }

        if (puesto.Estado != true)
        {
            return BadRequest(new
            {
                mensaje = "El puesto ya está inactivo."
            });
        }

        // ========================================
        // PROTEGER EMPLEADOS ACTIVOS
        // ========================================

        bool tieneEmpleadosActivos = await _context.Empleados
            .AnyAsync(e =>
                e.IdPuesto == id &&
                e.Estado == true);

        if (tieneEmpleadosActivos)
        {
            return Conflict(new
            {
                mensaje = "No se puede desactivar este puesto porque tiene empleados activos asignados. Primero debes reasignarlos o desactivarlos."
            });
        }

        // ========================================
        // DESACTIVACIÓN LÓGICA
        // ========================================

        puesto.Estado = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Puesto desactivado correctamente.",
            puesto.IdPuesto,
            puesto.Estado
        });
    }

    // ========================================
    // PUT: ACTIVAR PUESTO
    // ========================================

    [HttpPut("{id:int}/activar")]
    public async Task<IActionResult> ActivarPuesto(int id)
    {
        var puesto = await _context.Puestos
            .FirstOrDefaultAsync(p => p.IdPuesto == id);

        if (puesto == null)
        {
            return NotFound(new
            {
                mensaje = "El puesto no existe."
            });
        }

        if (puesto.Estado == true)
        {
            return BadRequest(new
            {
                mensaje = "El puesto ya está activo."
            });
        }

        puesto.Estado = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Puesto activado correctamente.",
            puesto.IdPuesto,
            puesto.Estado
        });
    }

    // ========================================
    // FUNCIONES AUXILIARES
    // ========================================

    private static string NormalizarNombre(string nombre)
    {
        return Regex.Replace(nombre.Trim(), @"\s+", " ");
    }

    private static string? Limpiar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        return valor.Trim();
    }

    private static bool EsDuplicado(DbUpdateException error)
    {
        return error.InnerException is MySqlException mysql &&
               mysql.Number == 1062;
    }
}