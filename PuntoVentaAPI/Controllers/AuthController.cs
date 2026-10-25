using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PuntoVentaAPI.Controllers;

// ============================================
// DATOS PARA INICIAR SESIÓN
// ============================================
public class LoginDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = string.Empty;
}

// ============================================
// CAMBIAR CONTRASEÑA EN EL PRIMER INGRESO
// ============================================
public class CambiarContrasenaInicialDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña temporal.")]
    public string ContrasenaTemporal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu nueva contraseña.")]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu nueva contraseña.")]
    [Compare(nameof(NuevaContrasena),
        ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}

// ============================================
// DATOS DEL ADMINISTRADOR INICIAL
// ============================================
public class AdministradorInicialDto
{
    [Required]
    public string ClaveInstalacion { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int IdEmpleado { get; set; }

    [Required]
    [RegularExpression(
        @"^[A-Za-z][A-Za-z0-9._-]{3,49}$",
        ErrorMessage = "El usuario debe tener entre 4 y 50 caracteres y comenzar con una letra.")]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Contrasena { get; set; } = string.Empty;
}

// ============================================
// CONTROLADOR DE AUTENTICACIÓN
// ============================================
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _ambiente;

    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthController(
        PuntoVentaDbContext context,
        IConfiguration config,
        IWebHostEnvironment ambiente)
    {
        _context = context;
        _config = config;
        _ambiente = ambiente;
    }

    // ========================================
    // POST: CREAR ADMINISTRADOR INICIAL
    // ========================================
    [HttpPost("inicializar")]
    [AllowAnonymous]
    public async Task<IActionResult> Inicializar(
        [FromBody] AdministradorInicialDto datos)
    {
        // Solo disponible en desarrollo.
        if (!_ambiente.IsDevelopment())
            return NotFound();

        string? claveConfigurada = _config["Setup:Key"];

        if (string.IsNullOrWhiteSpace(claveConfigurada) ||
            !CoincidenClaves(
                datos.ClaveInstalacion,
                claveConfigurada))
        {
            return Unauthorized(new
            {
                mensaje = "La clave de instalación es incorrecta."
            });
        }

        // No permitir un segundo administrador inicial.
        bool existeAdministrador = await _context.Usuarios
            .AnyAsync(u => u.Rol == "ADMIN");

        if (existeAdministrador)
        {
            return Conflict(new
            {
                mensaje = "El administrador inicial ya existe."
            });
        }

        string? errorContrasena =
            ValidarContrasena(datos.Contrasena);

        if (errorContrasena != null)
        {
            return BadRequest(new
            {
                mensaje = errorContrasena
            });
        }

        // Comprobar que el empleado exista y esté activo.
        var empleado = await _context.Empleados
            .Include(e => e.IdPuestoNavigation)
            .FirstOrDefaultAsync(
                e => e.IdEmpleado == datos.IdEmpleado);

        if (empleado == null ||
            empleado.Estado != true ||
            empleado.IdPuestoNavigation.Estado != true)
        {
            return BadRequest(new
            {
                mensaje = "El empleado o su puesto no están activos."
            });
        }

        bool empleadoTieneCuenta = await _context.Usuarios
            .AnyAsync(u => u.IdEmpleado == datos.IdEmpleado);

        if (empleadoTieneCuenta)
        {
            return Conflict(new
            {
                mensaje = "Este empleado ya tiene un usuario registrado."
            });
        }

        string nombreUsuario = datos.Usuario.Trim();

        bool usuarioExiste = await _context.Usuarios
            .AnyAsync(u => u.Usuario1 == nombreUsuario);

        if (usuarioExiste)
        {
            return Conflict(new
            {
                mensaje = "El nombre de usuario ya está registrado."
            });
        }

        // Crear administrador inicial.
        // Esta cuenta de instalación no requiere cambio inicial.
        var usuario = new Usuario
        {
            IdEmpleado = datos.IdEmpleado,
            Usuario1 = nombreUsuario,
            Rol = "ADMIN",
            Estado = true,
            FechaCreacion = DateTime.Now,
            DebeCambiarContrasena = false
        };

        usuario.PasswordHash = _hasher.HashPassword(
            usuario,
            datos.Contrasena);

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Administrador inicial creado correctamente.",
            usuario.IdUsuario,
            usuario.Usuario1,
            usuario.Rol
        });
    }

    // ========================================
    // POST: INICIAR SESIÓN
    // ========================================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto datos)
    {
        string nombreUsuario = datos.Usuario.Trim();

        var usuario = await _context.Usuarios
            .Include(u => u.IdEmpleadoNavigation)
                .ThenInclude(e => e.IdPuestoNavigation)
            .FirstOrDefaultAsync(
                u => u.Usuario1 == nombreUsuario);

        // Verificar usuario, empleado y puesto.
        if (usuario == null ||
            usuario.Estado != true ||
            usuario.IdEmpleadoNavigation.Estado != true ||
            usuario.IdEmpleadoNavigation
                .IdPuestoNavigation.Estado != true)
        {
            return CredencialesIncorrectas();
        }

        // Verificar contraseña.
        PasswordVerificationResult resultado;

        try
        {
            resultado = _hasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                datos.Contrasena);
        }
        catch (FormatException)
        {
            return CredencialesIncorrectas();
        }

        if (resultado == PasswordVerificationResult.Failed)
        {
            return CredencialesIncorrectas();
        }

        // Actualizar el hash si fuera necesario.
        if (resultado ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = _hasher.HashPassword(
                usuario,
                datos.Contrasena);
        }

        // ====================================
        // VERIFICAR CONTRASEÑA TEMPORAL
        // ====================================
        if (usuario.DebeCambiarContrasena)
        {
            if (resultado ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _context.SaveChangesAsync();
            }

            // No entregar JWT mientras el usuario
            // tenga pendiente el cambio de contraseña.
            Response.Headers["Cache-Control"] = "no-store";

            return Ok(new
            {
                mensaje = "Debes cambiar tu contraseña temporal antes de ingresar al sistema.",
                requiereCambioContrasena = true,
                usuario = usuario.Usuario1
            });
        }

        // ====================================
        // INICIO DE SESIÓN NORMAL
        // ====================================
        usuario.UltimoAcceso = DateTime.Now;

        await _context.SaveChangesAsync();

        // ====================================
        // GENERAR TOKEN JWT
        // ====================================
        DateTime vencimiento =
            DateTime.UtcNow.AddHours(4);

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuario.IdUsuario.ToString()),

            new Claim(
                ClaimTypes.Name,
                usuario.Usuario1),

            new Claim(
                ClaimTypes.Role,
                usuario.Rol),

            new Claim(
                "idEmpleado",
                usuario.IdEmpleado.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        string? clave = _config["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            return StatusCode(500, new
            {
                mensaje = "No está configurada la seguridad JWT."
            });
        }

        var seguridad = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(clave));

        var credenciales = new SigningCredentials(
            seguridad,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: "PuntoVentaAPI",
            audience: "PuntoVentaWeb",
            claims: claims,
            expires: vencimiento,
            signingCredentials: credenciales);

        string token = new JwtSecurityTokenHandler()
            .WriteToken(jwt);

        Response.Headers["Cache-Control"] = "no-store";

        return Ok(new
        {
            mensaje = "Inicio de sesión correcto.",
            requiereCambioContrasena = false,
            token,
            expiracionUtc = vencimiento,
            idUsuario = usuario.IdUsuario,
            idEmpleado = usuario.IdEmpleado,
            usuario = usuario.Usuario1,
            nombreCompleto =
                usuario.IdEmpleadoNavigation.Nombre + " " +
                usuario.IdEmpleadoNavigation.Apellido,
            rol = usuario.Rol
        });
    }

    // ========================================
    // POST: CAMBIAR CONTRASEÑA INICIAL
    // ========================================
    [HttpPost("cambiar-contrasena-inicial")]
    [AllowAnonymous]
    public async Task<IActionResult> CambiarContrasenaInicial(
        [FromBody] CambiarContrasenaInicialDto datos)
    {
        Response.Headers["Cache-Control"] = "no-store";

        string nombreUsuario = datos.Usuario.Trim();

        // Consultar la cuenta.
        var usuario = await _context.Usuarios
            .Include(u => u.IdEmpleadoNavigation)
                .ThenInclude(e => e.IdPuestoNavigation)
            .FirstOrDefaultAsync(
                u => u.Usuario1 == nombreUsuario);

        // Verificar que la cuenta continúe activa.
        if (usuario == null ||
            usuario.Estado != true ||
            usuario.IdEmpleadoNavigation.Estado != true ||
            usuario.IdEmpleadoNavigation
                .IdPuestoNavigation.Estado != true)
        {
            return CredencialesIncorrectas();
        }

        // Verificar nuevamente la contraseña temporal.
        // No se acepta una solicitud basada solamente
        // en el nombre de usuario.
        PasswordVerificationResult resultado;

        try
        {
            resultado = _hasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                datos.ContrasenaTemporal);
        }
        catch (FormatException)
        {
            return CredencialesIncorrectas();
        }

        if (resultado == PasswordVerificationResult.Failed)
        {
            return CredencialesIncorrectas();
        }

        // El cambio inicial debe estar pendiente.
        if (!usuario.DebeCambiarContrasena)
        {
            return BadRequest(new
            {
                mensaje = "Esta cuenta no tiene pendiente un cambio inicial de contraseña."
            });
        }

        // Validar la nueva contraseña.
        string? errorContrasena =
            ValidarContrasena(datos.NuevaContrasena);

        if (errorContrasena != null)
        {
            return BadRequest(new
            {
                mensaje = errorContrasena
            });
        }

        // Evitar utilizar nuevamente la contraseña temporal.
        if (string.Equals(
            datos.ContrasenaTemporal,
            datos.NuevaContrasena,
            StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                mensaje = "La nueva contraseña debe ser diferente de la contraseña temporal."
            });
        }

        // Guardar la nueva contraseña protegida.
        usuario.PasswordHash = _hasher.HashPassword(
            usuario,
            datos.NuevaContrasena);

        // Marcar que el cambio inicial fue completado.
        usuario.DebeCambiarContrasena = false;

        await _context.SaveChangesAsync();

        // No iniciar sesión automáticamente.
        // El usuario debe autenticarse con su nueva contraseña.
        return Ok(new
        {
            mensaje = "Contraseña personal establecida correctamente. Ahora puedes iniciar sesión con tu nueva contraseña."
        });
    }

    // ========================================
    // GET: PERFIL DEL USUARIO AUTENTICADO
    // ========================================
    [HttpGet("perfil")]
    [Authorize]
    public IActionResult Perfil()
    {
        return Ok(new
        {
            idUsuario = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            usuario = User.FindFirstValue(
                ClaimTypes.Name),

            rol = User.FindFirstValue(
                ClaimTypes.Role),

            idEmpleado = User.FindFirstValue(
                "idEmpleado")
        });
    }

    // ========================================
    // VALIDACIÓN DE CONTRASEÑA
    // ========================================
    private static string? ValidarContrasena(
        string contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena) ||
            contrasena.Length < 12 ||
            contrasena.Length > 128)
        {
            return "La contraseña debe tener entre 12 y 128 caracteres.";
        }

        if (!contrasena.Any(char.IsUpper) ||
            !contrasena.Any(char.IsLower) ||
            !contrasena.Any(char.IsDigit) ||
            !contrasena.Any(c =>
                char.IsPunctuation(c) || char.IsSymbol(c)))
        {
            return "La contraseña debe contener mayúsculas, minúsculas, números y un carácter especial.";
        }

        return null;
    }

    // ========================================
    // COMPARAR CLAVES
    // ========================================
    private static bool CoincidenClaves(
        string ingresada,
        string configurada)
    {
        byte[] a = Encoding.UTF8.GetBytes(ingresada);
        byte[] b = Encoding.UTF8.GetBytes(configurada);

        if (a.Length != b.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    // ========================================
    // RESPUESTA DE CREDENCIALES INCORRECTAS
    // ========================================
    private IActionResult CredencialesIncorrectas()
    {
        return Unauthorized(new
        {
            mensaje = "Usuario o contraseña incorrectos."
        });
    }
}