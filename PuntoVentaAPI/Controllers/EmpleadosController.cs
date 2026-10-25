using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace PuntoVentaAPI.Controllers;

// ============================================
// DTO PARA CREAR Y EDITAR EMPLEADOS
// ============================================

public class GuardarEmpleadoDto : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150)]
    [RegularExpression(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
        ErrorMessage = "Ingresa un correo válido.")]
    public string? Correo { get; set; }

    [StringLength(255)]
    public string? Direccion { get; set; }

    public DateOnly FechaIngreso { get; set; } =
        DateOnly.FromDateTime(DateTime.Today);

    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona un puesto.")]
    public int IdPuesto { get; set; }

    // ========================================
    // VALIDACIONES PERSONALIZADAS
    // ========================================

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        foreach (var error in ValidarNombre(
            Nombre, nameof(Nombre), "nombre"))
        {
            yield return error;
        }

        foreach (var error in ValidarNombre(
            Apellido, nameof(Apellido), "apellido"))
        {
            yield return error;
        }

        if (!string.IsNullOrWhiteSpace(Telefono))
        {
            bool valido = Regex.IsMatch(
                Telefono.Trim(),
                @"^(?:\+502[ -]?)?[2-7][0-9]{3}[ -]?[0-9]{4}$");

            if (!valido)
            {
                yield return new ValidationResult(
                    "El teléfono debe tener 8 dígitos válidos de Guatemala.",
                    new[] { nameof(Telefono) });
            }
        }

        if (!string.IsNullOrWhiteSpace(Direccion))
        {
            string direccion = Direccion.Trim();

            if (direccion.Length < 5 ||
                !direccion.Any(char.IsLetter))
            {
                yield return new ValidationResult(
                    "La dirección debe tener al menos 5 caracteres y contener letras.",
                    new[] { nameof(Direccion) });
            }
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var fechaMinima = new DateOnly(1900, 1, 1);

        if (FechaIngreso < fechaMinima ||
            FechaIngreso > hoy)
        {
            yield return new ValidationResult(
                "La fecha de ingreso debe ser válida y no puede ser futura.",
                new[] { nameof(FechaIngreso) });
        }
    }

    private static IEnumerable<ValidationResult> ValidarNombre(
        string? valor,
        string propiedad,
        string etiqueta)
    {
        if (string.IsNullOrWhiteSpace(valor))
            yield break;

        string texto = valor.Trim();

        if (texto.Length < 2 || texto.Length > 100)
        {
            yield return new ValidationResult(
                $"El {etiqueta} debe tener entre 2 y 100 caracteres.",
                new[] { propiedad });
        }
        else if (!Regex.IsMatch(
            texto,
            @"^[\p{L}\p{M}]+(?:[ '\-][\p{L}\p{M}]+)*$"))
        {
            yield return new ValidationResult(
                $"El {etiqueta} solo permite letras, espacios, guiones y apóstrofes.",
                new[] { propiedad });
        }
    }
}

// ============================================
// CONTROLADOR DE EMPLEADOS
// ============================================

// ADMIN administra empleados.
//
// VENDEDOR únicamente puede consultar
// los datos básicos de su propio empleado.
//
// CONSULTA no tiene acceso a este controlador.

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,VENDEDOR")]
public class EmpleadosController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public EmpleadosController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // GET: CONSULTAR EMPLEADOS
    // ========================================

    [HttpGet]
    public async Task<IActionResult> ObtenerEmpleados()
    {
        // ====================================
        // VENDEDOR: SOLO SU PROPIO EMPLEADO
        // ====================================

        if (User.IsInRole("VENDEDOR"))
        {
            // El JWT contiene el ID del usuario.
            // Obtenemos ese ID para identificar
            // al empleado asociado en MySQL.

            string? identificador = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                    identificador,
                    out int idUsuario) ||
                idUsuario <= 0)
            {
                return Forbid();
            }

            // Consultar únicamente el empleado
            // asociado al usuario autenticado.

            var empleadoSesion = await _context.Usuarios
                .AsNoTracking()
                .Where(u =>
                    u.IdUsuario == idUsuario &&
                    u.Rol == "VENDEDOR" &&
                    u.Estado == true &&
                    u.IdEmpleadoNavigation.Estado == true &&
                    u.IdEmpleadoNavigation
                        .IdPuestoNavigation.Estado == true)
                .Select(u => new
                {
                    IdEmpleado = u.IdEmpleado,
                    Nombre = u.IdEmpleadoNavigation.Nombre,
                    Apellido = u.IdEmpleadoNavigation.Apellido,
                    Estado = u.IdEmpleadoNavigation.Estado
                })
                .FirstOrDefaultAsync();

            if (empleadoSesion == null)
            {
                return Forbid();
            }

            // Devolver una lista para mantener
            // la compatibilidad con Ventas.razor.

            return Ok(new[] { empleadoSesion });
        }

        // ====================================
        // ADMIN: LISTADO COMPLETO
        // ====================================

        var empleados = await _context.Empleados
            .AsNoTracking()
            .OrderBy(e => e.IdEmpleado)
            .Select(e => new
            {
                e.IdEmpleado,
                e.Nombre,
                e.Apellido,
                e.Telefono,
                e.Correo,
                e.Direccion,
                e.FechaIngreso,
                e.IdPuesto,
                NombrePuesto = e.IdPuestoNavigation.Nombre,
                e.Estado
            })
            .ToListAsync();

        return Ok(empleados);
    }

    // ========================================
    // GET: EMPLEADO POR ID
    // ========================================

    [HttpGet("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ObtenerEmpleadoPorId(int id)
    {
        var empleado = await _context.Empleados
            .AsNoTracking()
            .Where(e => e.IdEmpleado == id)
            .Select(e => new
            {
                e.IdEmpleado,
                e.Nombre,
                e.Apellido,
                e.Telefono,
                e.Correo,
                e.Direccion,
                e.FechaIngreso,
                e.IdPuesto,
                NombrePuesto = e.IdPuestoNavigation.Nombre,
                e.Estado
            })
            .FirstOrDefaultAsync();

        if (empleado == null)
        {
            return NotFound(new
            {
                mensaje = "El empleado no existe."
            });
        }

        return Ok(empleado);
    }

    // ========================================
    // POST: REGISTRAR EMPLEADO
    // ========================================

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RegistrarEmpleado(
        [FromBody] GuardarEmpleadoDto datos)
    {
        bool puestoActivo = await _context.Puestos.AnyAsync(
            p => p.IdPuesto == datos.IdPuesto &&
                 p.Estado == true);

        if (!puestoActivo)
        {
            return BadRequest(new
            {
                mensaje = "Selecciona un puesto activo y válido."
            });
        }

        string? correo =
            Limpiar(datos.Correo)?.ToLowerInvariant();

        if (await CorreoDuplicado(correo))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un empleado con ese correo."
            });
        }

        var empleado = new Empleado
        {
            Nombre = datos.Nombre.Trim(),
            Apellido = datos.Apellido.Trim(),
            Telefono = NormalizarTelefono(datos.Telefono),
            Correo = correo,
            Direccion = Limpiar(datos.Direccion),
            FechaIngreso = datos.FechaIngreso,
            IdPuesto = datos.IdPuesto,
            Estado = true
        };

        _context.Empleados.Add(empleado);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (EsDuplicado(ex))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un empleado con ese correo."
            });
        }

        return CreatedAtAction(
            nameof(ObtenerEmpleadoPorId),
            new { id = empleado.IdEmpleado },
            new
            {
                mensaje = "Empleado registrado correctamente.",
                empleado.IdEmpleado
            });
    }

    // ========================================
    // PUT: ACTUALIZAR EMPLEADO
    // ========================================

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActualizarEmpleado(
        int id,
        [FromBody] GuardarEmpleadoDto datos)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        if (empleado == null)
        {
            return NotFound(new
            {
                mensaje = "El empleado no existe."
            });
        }

        // Permitir conservar el puesto anterior
        // aunque haya quedado inactivo.
        //
        // No permitir asignar otro puesto
        // que esté inactivo.

        bool puestoValido = await _context.Puestos
            .AnyAsync(p =>
                p.IdPuesto == datos.IdPuesto &&
                (p.Estado == true ||
                 p.IdPuesto == empleado.IdPuesto));

        if (!puestoValido)
        {
            return BadRequest(new
            {
                mensaje = "Selecciona un puesto activo."
            });
        }

        string? correo =
            Limpiar(datos.Correo)?.ToLowerInvariant();

        if (await CorreoDuplicado(correo, id))
        {
            return Conflict(new
            {
                mensaje = "El correo pertenece a otro empleado."
            });
        }

        empleado.Nombre = datos.Nombre.Trim();
        empleado.Apellido = datos.Apellido.Trim();
        empleado.Telefono = NormalizarTelefono(datos.Telefono);
        empleado.Correo = correo;
        empleado.Direccion = Limpiar(datos.Direccion);
        empleado.FechaIngreso = datos.FechaIngreso;
        empleado.IdPuesto = datos.IdPuesto;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (EsDuplicado(ex))
        {
            return Conflict(new
            {
                mensaje = "El correo ya pertenece a otro empleado."
            });
        }

        return Ok(new
        {
            mensaje = "Empleado actualizado correctamente.",
            empleado.IdEmpleado
        });
    }

    // ========================================
    // DELETE: DESACTIVAR EMPLEADO
    // ========================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DesactivarEmpleado(int id)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        if (empleado == null)
        {
            return NotFound(new
            {
                mensaje = "El empleado no existe."
            });
        }

        if (empleado.Estado != true)
        {
            return BadRequest(new
            {
                mensaje = "El empleado ya está inactivo."
            });
        }

        // ====================================
        // PROTEGER CUENTAS DE USUARIO ACTIVAS
        // ====================================

        bool tieneUsuarioActivo = await _context.Usuarios
            .AnyAsync(u =>
                u.IdEmpleado == id &&
                u.Estado == true);

        if (tieneUsuarioActivo)
        {
            return Conflict(new
            {
                mensaje = "No se puede desactivar al empleado porque tiene una cuenta de usuario activa. Desactiva primero esa cuenta."
            });
        }

        // ====================================
        // NO BORRAR EL HISTORIAL DE VENTAS
        // ====================================

        empleado.Estado = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Empleado desactivado correctamente.",
            empleado.IdEmpleado,
            empleado.Estado
        });
    }

    // ========================================
    // PUT: REACTIVAR EMPLEADO
    // ========================================

    [HttpPut("{id:int}/activar")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActivarEmpleado(int id)
    {
        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        if (empleado == null)
        {
            return NotFound(new
            {
                mensaje = "El empleado no existe."
            });
        }

        if (empleado.Estado == true)
        {
            return BadRequest(new
            {
                mensaje = "El empleado ya está activo."
            });
        }

        bool puestoActivo = await _context.Puestos
            .AnyAsync(p =>
                p.IdPuesto == empleado.IdPuesto &&
                p.Estado == true);

        if (!puestoActivo)
        {
            return Conflict(new
            {
                mensaje = "No se puede activar al empleado porque su puesto está inactivo. Activa el puesto o cambia la asignación."
            });
        }

        empleado.Estado = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Empleado activado correctamente.",
            empleado.IdEmpleado,
            empleado.Estado
        });
    }

    // ========================================
    // VALIDAR CORREO REPETIDO
    // ========================================

    private async Task<bool> CorreoDuplicado(
        string? correo,
        int? excluirId = null)
    {
        if (correo == null)
            return false;

        return await _context.Empleados.AnyAsync(e =>
            e.Correo == correo &&
            (!excluirId.HasValue ||
             e.IdEmpleado != excluirId.Value));
    }

    // ========================================
    // NORMALIZAR TELÉFONO
    // ========================================

    private static string? NormalizarTelefono(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        string numero = Regex.Replace(
            valor.Trim(),
            @"[\s-]",
            "");

        if (numero.StartsWith("+502"))
            numero = numero.Substring(4);

        if (numero.Length == 8)
            return numero.Insert(4, "-");

        return valor.Trim();
    }

    // ========================================
    // LIMPIAR CAMPOS OPCIONALES
    // ========================================

    private static string? Limpiar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    // ========================================
    // IDENTIFICAR DUPLICADOS DE MYSQL
    // ========================================

    private static bool EsDuplicado(DbUpdateException ex)
    {
        return ex.InnerException is MySqlException mysql &&
               mysql.Number == 1062;
    }
}