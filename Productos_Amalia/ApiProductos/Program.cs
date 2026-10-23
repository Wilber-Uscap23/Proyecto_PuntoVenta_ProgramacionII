using System.Text;
using ApiProductos.Data;
using ApiProductos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Base de datos (MySQL) ----------
var cadenaConexion = builder.Configuration.GetConnectionString("PuntoVentaDB")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'PuntoVentaDB' en appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(cadenaConexion, new MySqlServerVersion(new Version(8, 0, 36))));

// ---------- JWT (misma sección "Jwt" que usa el módulo de Login de Wilber) ----------
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        ValidateLifetime = true
    });
builder.Services.AddAuthorization();

// ---------- Carpeta de imágenes ----------
// Por defecto: <carpeta de la API>/wwwroot/images/productos.
// En IIS conviene poner en "Imagenes:Carpeta" una ruta FUERA de la carpeta publicada
// (ej. C:\PuntoVenta\imagenes\productos) para que no se borren al volver a publicar.
var carpetaConfig = builder.Configuration["Imagenes:Carpeta"];
var carpetaImagenes = string.IsNullOrWhiteSpace(carpetaConfig)
    ? Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "images", "productos")
    : Path.GetFullPath(carpetaConfig, builder.Environment.ContentRootPath);
Directory.CreateDirectory(carpetaImagenes);
builder.Services.AddSingleton(new AlmacenImagenes(carpetaImagenes));

builder.Services.AddControllers();

var app = builder.Build();

// Las imágenes se sirven públicamente (una etiqueta <img> no puede enviar el token JWT).
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(carpetaImagenes),
    RequestPath = AlmacenImagenes.RutaPublica
});

app.UseAuthentication();
app.UseAuthorization();

var endpoints = app.MapControllers();

// Solo para probar este módulo sin el Login de Wilber (appsettings.Development.json).
// En producción "Seguridad:ExigirJwt" es true y todos los endpoints exigen token.
if (!builder.Configuration.GetValue("Seguridad:ExigirJwt", true))
{
    endpoints.AllowAnonymous();
}

app.Run();
