using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;

namespace PuntoVentaAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;

    public MenuController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ============================================
    // OBTENER OPCIONES DEL MENÚ SEGÚN EL ROL
    // ============================================
    [HttpGet("mis-opciones")]
    public async Task<IActionResult> ObtenerMisOpciones()
    {
        // Obtener el rol desde el JWT del usuario.
        string? rol = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(rol))
        {
            return Unauthorized(new
            {
                mensaje = "No se pudo identificar el rol del usuario."
            });
        }

        // Consultar las opciones autorizadas en MySQL.
        var opciones = await _context.Menus
            .FromSqlInterpolated($@"
                SELECT m.*
                FROM menu AS m
                INNER JOIN menu_rol AS mr
                    ON mr.id_menu = m.id_menu
                WHERE mr.rol = {rol}
                  AND m.estado = 1
            ")
            .AsNoTracking()
            .OrderBy(m => m.Orden)
            .ThenBy(m => m.IdMenu)
            .Select(m => new
            {
                m.IdMenu,
                m.Nombre,
                Ruta = m.Ruta ?? "",
                Icono = m.Icono ?? "",
                m.IdMenuPadre,
                m.Orden
            })
            .ToListAsync();

        return Ok(opciones);
    }
}