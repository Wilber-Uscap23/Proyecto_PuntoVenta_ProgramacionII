using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Seguridad;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.Text;

// ============================================
// CREAR LA APLICACIÓN
// ============================================

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURACIÓN DE SERVICIOS
// ============================================

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// ============================================
// COMPROBANTES DE VENTA EN PDF
// ============================================

QuestPDF.Settings.License = LicenseType.Community;

// ============================================
// CONEXIÓN CON MYSQL
// ============================================

// La conexión se obtiene desde la configuración.
// En desarrollo usamos User Secrets.

string? cadenaConexion = builder.Configuration
    .GetConnectionString("ConexionMySQL");

if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión de MySQL.");
}

// Registrar Entity Framework Core con MySQL.

builder.Services.AddDbContext<PuntoVentaDbContext>(
    options =>
    {
        options.UseMySql(
            cadenaConexion,
            ServerVersion.AutoDetect(cadenaConexion)
        );
    }
);

// ============================================
// CONFIGURACIÓN DE SEGURIDAD JWT
// ============================================

// Recuperar la clave desde User Secrets.

string? jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "No se encontró una clave JWT válida en User Secrets.");
}

// Configurar autenticación mediante JWT Bearer.

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Reconocer los tipos de claims
        // utilizados por AuthController.

        options.MapInboundClaims = true;

        // ====================================
        // REGLAS DE VALIDACIÓN DEL TOKEN
        // ====================================

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Validar quién emitió el token.
                ValidateIssuer = true,
                ValidIssuer = "PuntoVentaAPI",

                // Validar para quién fue emitido.
                ValidateAudience = true,
                ValidAudience = "PuntoVentaWeb",

                // Validar fecha de vencimiento.
                ValidateLifetime = true,
                RequireExpirationTime = true,

                // Validar firma de seguridad.
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                // Margen permitido para el reloj.
                ClockSkew = TimeSpan.FromSeconds(30),

                // Claims utilizados para
                // identificar usuario y rol.
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

        // ====================================
        // VALIDAR QUE LA CUENTA SIGA ACTIVA
        // ====================================

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // Obtener el ID del usuario
                // almacenado dentro del JWT.

                string? idTexto = context.Principal?
                    .FindFirst(ClaimTypes.NameIdentifier)?
                    .Value;

                // Obtener el rol del usuario.

                string? rolToken = context.Principal?
                    .FindFirst(ClaimTypes.Role)?
                    .Value;

                // Validar los datos del token.

                if (!int.TryParse(
                        idTexto,
                        out int idUsuario) ||
                    string.IsNullOrWhiteSpace(rolToken))
                {
                    context.Fail(
                        "El token no contiene un usuario válido.");

                    return;
                }

                // Obtener el contexto de MySQL.

                var db = context.HttpContext
                    .RequestServices
                    .GetRequiredService<PuntoVentaDbContext>();

                // =================================
                // COMPROBAR ESTADOS EN MYSQL
                // =================================

                bool usuarioActivo = await db.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.IdUsuario == idUsuario &&

                        // El rol no debe haber cambiado.
                        u.Rol == rolToken &&

                        // Usuario activo.
                        u.Estado == true &&

                        // Empleado activo.
                        u.IdEmpleadoNavigation.Estado == true &&

                        // Puesto activo.
                        u.IdEmpleadoNavigation
                            .IdPuestoNavigation.Estado == true &&

                        // =================================
                        // CAMBIO DE CONTRASEÑA OBLIGATORIO
                        // =================================
                        // Si el administrador restablece
                        // una contraseña, cualquier token
                        // anterior dejará de funcionar.

                        u.DebeCambiarContrasena == false
                    );

                // Si se desactiva el usuario,
                // empleado o puesto, cambia el rol
                // o tiene pendiente el cambio de
                // contraseña, se rechaza el token.

                if (!usuarioActivo)
                {
                    context.Fail(
                        "La cuenta no está autorizada para utilizar esta sesión.");
                }
            }
        };
    });

// ============================================
// CONFIGURACIÓN DE AUTORIZACIÓN
// ============================================

// Permite utilizar:
// [Authorize]
// [Authorize(Roles = "ADMIN")]

builder.Services.AddAuthorization();

// ============================================
// CREAR LA APLICACIÓN WEB API
// ============================================

var app = builder.Build();

// ============================================
// CONFIGURACIÓN DE DESARROLLO
// ============================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ============================================
// CONFIGURACIÓN HTTP
// ============================================

app.UseHttpsRedirection();

app.UseRouting();

// ============================================
// PASO 1: AUTENTICACIÓN
// ============================================

// Verificar que el JWT sea válido.

app.UseAuthentication();

// ============================================
// PASO 2: PERMISOS POR ROL
// ============================================

// Aplicar las reglas que definimos para
// ADMIN, VENDEDOR y CONSULTA.

app.UseMiddleware<PermisosOperacionesMiddleware>();

// ============================================
// PASO 3: AUTORIZACIÓN
// ============================================

// Respetar las restricciones [Authorize]
// de los controladores.

app.UseAuthorization();

// ============================================
// PASO 4: REGISTRAR CONTROLADORES DE LA API
// ============================================

// Todas las rutas de los controladores
// requieren que el usuario esté autenticado.
//
// Las acciones que tengan [AllowAnonymous]
// podrán utilizarse sin iniciar sesión.
//
// Los permisos específicos por rol
// seguirán aplicándose.

app.MapControllers().RequireAuthorization();

// ============================================
// EJECUTAR LA API
// ============================================

app.Run();