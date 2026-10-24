// Módulo: Publicación IIS y red — Rodrigo Rodríguez
// Endpoint público para comprobar desde OTRA máquina virtual que la API
// responde y que tiene conexión con MySQL (no devuelve datos del negocio).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoVenta.API.Data;

namespace PuntoVenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public class SaludController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// GET api/salud — 200 si la API y la base de datos responden, 503 si la base de datos no responde.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool baseDatosOk;
        try
        {
            baseDatosOk = await db.Database.CanConnectAsync(ct);
        }
        catch
        {
            baseDatosOk = false;
        }

        var respuesta = new
        {
            estado = baseDatosOk ? "OK" : "ERROR",
            api = "PuntoVenta.API",
            servidor = Environment.MachineName,
            ambiente = env.EnvironmentName,
            baseDatos = baseDatosOk ? "Conectada" : "Sin conexión",
            fechaServidor = DateTime.Now
        };

        return baseDatosOk ? Ok(respuesta) : StatusCode(StatusCodes.Status503ServiceUnavailable, respuesta);
    }
}
