// =====================================================================
//  BASE DE INTEGRACIÓN — el Program.cs real de la API es de Wilber.
//
//  Para el módulo de Rodrigo, el Program.cs de Wilber solo necesita:
//    1) AddControllers()  + MapControllers()      (ya lo tiene)
//    2) Autenticación JWT + UseAuthentication()   (ya lo tiene)
//    3) La política CORS "PermitirWeb"           (solo si Blazor es WebAssembly
//       o si se consume la API desde otro sitio)
//  Los controladores MenuController, ReportesController y SaludController
//  se registran solos con MapControllers().
// =====================================================================
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuntoVenta.API.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------- Base de datos (MySQL) ----------
var cadenaConexion = builder.Configuration.GetConnectionString("PuntoVentaDB")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'PuntoVentaDB' en appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseMySQL(cadenaConexion));

// ---------- JWT ----------
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS ----------
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirWeb", policy =>
        policy.WithOrigins(origenesPermitidos)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Si el sitio de IIS solo tiene enlace HTTP (puerto 8080), esta línea no redirige nada.
app.UseHttpsRedirection();

app.UseCors("PermitirWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
