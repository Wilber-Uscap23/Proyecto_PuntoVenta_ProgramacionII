using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PuntoVentaAPI.Controllers;

// ============================================
// REGLAS DE CONTRASEÑAS
// ============================================
internal static class ReglasClaveUsuario
{
    public static string? Validar(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave) ||
            clave.Length < 12 ||
            clave.Length > 128)
        {
            return "La contraseña debe tener entre 12 y 128 caracteres.";
        }

        if (!clave.Any(char.IsUpper) ||
            !clave.Any(char.IsLower) ||
            !clave.Any(char.IsDigit) ||
            !clave.Any(c =>
                char.IsPunctuation(c) || char.IsSymbol(c)))
        {
            return "La contraseña necesita mayúsculas, minúsculas, números y un símbolo.";
        }

        return null;
    }
}

// ============================================
// REGISTRAR USUARIO
// ============================================
public class CrearUsuarioEntrada : IValidatableObject
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona un empleado.")]
    public int IdEmpleado { get; set; }

    // Se conserva para compatibilidad con el formulario.
    // La API utilizará el correo registrado del empleado.
    public string Usuario { get; set; } = "";

    [Required]
    [RegularExpression(
        "^(ADMIN|VENDEDOR|CONSULTA)$",
        ErrorMessage = "Selecciona un rol válido.")]
    public string Rol { get; set; } = "VENDEDOR";

    [Required]
    public string Contrasena { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        string? error = ReglasClaveUsuario.Validar(Contrasena);

        if (error != null)
        {
            yield return new ValidationResult(
                error,
                new[] { nameof(Contrasena) });
        }
    }
}

// ============================================
// EDITAR USUARIO
// ============================================
public class EditarUsuarioEntrada
{
    // Acepta correos y nombres antiguos para conservar
    // la compatibilidad con las cuentas existentes.
    [Required]
    [StringLength(150)]
    [RegularExpression(
        @"^(?:[A-Za-z][A-Za-z0-9._-]{3,49}|[^@\s]+@[^@\s]+\.[^@\s]+)$",
        ErrorMessage = "Ingresa un nombre de usuario o correo válido.")]
    public string Usuario { get; set; } = "";

    [Required]
    [RegularExpression(
        "^(ADMIN|VENDEDOR|CONSULTA)$",
        ErrorMessage = "Selecciona un rol válido.")]
    public string Rol { get; set; } = "";
}

// ============================================
// CAMBIAR CONTRASEÑA
// ============================================
public class CambiarClaveUsuarioEntrada : IValidatableObject
{
    [Required]
    public string NuevaContrasena { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        string? error = ReglasClaveUsuario.Validar(
            NuevaContrasena);

        if (error != null)
        {
            yield return new ValidationResult(
                error,
                new[] { nameof(NuevaContrasena) });
        }
    }
}

// ============================================
// ACTIVAR O DESACTIVAR
// ============================================
public class CambiarEstadoUsuarioEntrada
{
    public bool Activo { get; set; }
}

// ============================================
// CONTROLADOR PROTEGIDO PARA ADMIN
// ============================================
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UsuariosController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuariosController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // GET: LISTADO DE USUARIOS
    // ========================================
    [HttpGet]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Usuario1)
            .Select(u => new
            {
                u.IdUsuario,
                u.IdEmpleado,
                Usuario = u.Usuario1,
                NombreEmpleado =
                    u.IdEmpleadoNavigation.Nombre + " " +
                    u.IdEmpleadoNavigation.Apellido,
                NombrePuesto =
                    u.IdEmpleadoNavigation
                        .IdPuestoNavigation.Nombre,
                u.Rol,
                u.Estado,
                u.FechaCreacion,
                u.UltimoAcceso
            })
            .ToListAsync();

        // No devolver contraseñas ni hashes.
        return Ok(usuarios);
    }

    // ========================================
    // GET: EMPLEADOS ACTIVOS SIN USUARIO
    // ========================================
    [HttpGet("empleados-disponibles")]
    public async Task<IActionResult> ObtenerEmpleadosDisponibles()
    {
        var empleados = await _context.Empleados
            .AsNoTracking()
            .Where(e =>
                e.Estado == true &&
                e.IdPuestoNavigation.Estado == true &&
                !_context.Usuarios.Any(u =>
                    u.IdEmpleado == e.IdEmpleado))
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Apellido)
            .Select(e => new
            {
                e.IdEmpleado,
                NombreCompleto =
                    e.Nombre + " " + e.Apellido,
                Puesto = e.IdPuestoNavigation.Nombre,
                Correo = e.Correo
            })
            .ToListAsync();

        return Ok(empleados);
    }

    // ========================================
    // POST: CREAR CUENTA
    // ========================================
    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearUsuarioEntrada datos)
    {
        // Obtener los datos del empleado seleccionado.
        var empleado = await _context.Empleados
            .AsNoTracking()
            .Where(e =>
                e.IdEmpleado == datos.IdEmpleado &&
                e.Estado == true &&
                e.IdPuestoNavigation.Estado == true)
            .Select(e => new
            {
                e.Correo
            })
            .FirstOrDefaultAsync();

        if (empleado == null)
        {
            return BadRequest(new
            {
                mensaje = "Selecciona un empleado activo con un puesto activo."
            });
        }

        // El nombre de usuario será el correo del empleado.
        string nombreUsuario = (empleado.Correo ?? "")
            .Trim()
            .ToLowerInvariant();

        if (nombreUsuario.Length > 150 ||
            !new EmailAddressAttribute().IsValid(nombreUsuario))
        {
            return BadRequest(new
            {
                mensaje = "El empleado debe tener un correo empresarial válido."
            });
        }

        // Verificar que el empleado no tenga una cuenta.
        bool empleadoOcupado = await _context.Usuarios
            .AnyAsync(u =>
                u.IdEmpleado == datos.IdEmpleado);

        if (empleadoOcupado)
        {
            return Conflict(new
            {
                mensaje = "Este empleado ya tiene una cuenta. No se permiten cuentas duplicadas."
            });
        }

        // Verificar que el correo no esté registrado.
        bool usuarioOcupado = await _context.Usuarios
            .AnyAsync(u =>
                u.Usuario1 == nombreUsuario);

        if (usuarioOcupado)
        {
            return Conflict(new
            {
                mensaje = "El correo empresarial ya está registrado como usuario."
            });
        }

        // Crear usuario con contraseña temporal.
        var usuario = new Usuario
        {
            IdEmpleado = datos.IdEmpleado,
            Usuario1 = nombreUsuario,
            Rol = datos.Rol,
            Estado = true,
            FechaCreacion = DateTime.Now,
            DebeCambiarContrasena = true
        };

        // Guardar la contraseña de manera segura.
        usuario.PasswordHash = _hasher.HashPassword(
            usuario,
            datos.Contrasena);

        _context.Usuarios.Add(usuario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensaje = "No se pudo crear el usuario. Verifica que el empleado y el correo empresarial no estén repetidos."
            });
        }

        return Ok(new
        {
            mensaje = "Usuario creado correctamente. Deberá cambiar su contraseña en el primer inicio de sesión.",
            usuario.IdUsuario,
            Usuario = usuario.Usuario1,
            usuario.Rol
        });
    }

    // ========================================
    // PUT: EDITAR NOMBRE Y ROL
    // ========================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(
        int id,
        [FromBody] EditarUsuarioEntrada datos)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.IdUsuario == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        string nombreNuevo = datos.Usuario.Trim();

        bool repetido = await _context.Usuarios
            .AnyAsync(u =>
                u.IdUsuario != id &&
                u.Usuario1 == nombreNuevo);

        if (repetido)
        {
            return Conflict(new
            {
                mensaje = "Ya existe otra cuenta con ese nombre de usuario."
            });
        }

        int idActual = UsuarioActualId();

        // Evitar que una cuenta administradora
        // se cambie a sí misma su nombre o rol
        // mientras mantiene una sesión abierta.
        if (id == idActual &&
            (usuario.Usuario1 != nombreNuevo ||
             usuario.Rol != datos.Rol))
        {
            return Conflict(new
            {
                mensaje = "No puedes modificar el nombre ni el rol de tu propia cuenta desde esta pantalla."
            });
        }

        if (usuario.Rol == "ADMIN" &&
            datos.Rol != "ADMIN" &&
            usuario.Estado == true)
        {
            int administradoresActivos =
                await _context.Usuarios.CountAsync(u =>
                    u.Rol == "ADMIN" &&
                    u.Estado == true);

            if (administradoresActivos <= 1)
            {
                return Conflict(new
                {
                    mensaje = "No puedes quitar el rol del último administrador activo."
                });
            }
        }

        usuario.Usuario1 = nombreNuevo;
        usuario.Rol = datos.Rol;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                mensaje = "No se pudo actualizar el usuario. Verifica que el nombre no esté repetido."
            });
        }

        return Ok(new
        {
            mensaje = "Usuario actualizado correctamente."
        });
    }

    // ========================================
    // PUT: RESTABLECER CONTRASEÑA
    // ========================================
    [HttpPut("{id:int}/contrasena")]
    public async Task<IActionResult> CambiarContrasena(
        int id,
        [FromBody] CambiarClaveUsuarioEntrada datos)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.IdUsuario == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        // El administrador asigna una contraseña temporal.
        usuario.PasswordHash = _hasher.HashPassword(
            usuario,
            datos.NuevaContrasena);

        // Exigir cambio de contraseña después del restablecimiento.
        usuario.DebeCambiarContrasena = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Contraseña restablecida correctamente. El usuario deberá cambiarla al iniciar sesión."
        });
    }

    // ========================================
    // PUT: ACTIVAR O DESACTIVAR CUENTA
    // ========================================
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoUsuarioEntrada datos)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.IdUsuario == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        if (!datos.Activo)
        {
            if (usuario.IdUsuario == UsuarioActualId())
            {
                return Conflict(new
                {
                    mensaje = "No puedes desactivar tu propia cuenta."
                });
            }

            if (usuario.Rol == "ADMIN" &&
                usuario.Estado == true)
            {
                int administradoresActivos =
                    await _context.Usuarios.CountAsync(u =>
                        u.Rol == "ADMIN" &&
                        u.Estado == true);

                if (administradoresActivos <= 1)
                {
                    return Conflict(new
                    {
                        mensaje = "Debe permanecer al menos un administrador activo."
                    });
                }
            }
        }
        else
        {
            // Comprobar nuevamente empleado y puesto.
            bool empleadoValido = await _context.Empleados
                .AnyAsync(e =>
                    e.IdEmpleado == usuario.IdEmpleado &&
                    e.Estado == true &&
                    e.IdPuestoNavigation.Estado == true);

            if (!empleadoValido)
            {
                return Conflict(new
                {
                    mensaje = "No puedes activar la cuenta porque su empleado o puesto está inactivo."
                });
            }
        }

        usuario.Estado = datos.Activo;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = datos.Activo
                ? "Usuario activado correctamente."
                : "Usuario desactivado correctamente."
        });
    }

    // ========================================
    // ID DEL ADMINISTRADOR CONECTADO
    // ========================================
    private int UsuarioActualId()
    {
        string? id = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(id, out int resultado)
            ? resultado
            : -1;
    }
}