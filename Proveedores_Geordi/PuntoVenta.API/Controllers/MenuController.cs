// Módulo: Menú dinámico — Rodrigo Rodríguez
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.API.Data;
using PuntoVenta.API.Services;
using PuntoVenta.Shared.Dtos.Menu;
using PuntoVenta.Shared.Dtos.Reportes;

namespace PuntoVenta.API.Controllers;

/// <summary>
/// Opciones del menú dinámico. Blazor arma el menú lateral con lo que devuelve este controlador,
/// así que las opciones NO se escriben en el código: se administran en la tabla `menu`.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class MenuController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// GET api/menu — Árbol del menú (solo opciones activas, ordenadas).
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<MenuItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MenuItemDto>>> ObtenerArbol(CancellationToken ct)
    {
        var opciones = await db.Menus.AsNoTracking().ToListAsync(ct);
        return Ok(MenuArbol.Construir(opciones));
    }

    /// <summary>
    /// GET api/menu/lista — Todas las opciones en forma plana (incluye las inactivas).
    /// Útil para revisar o administrar la tabla.
    /// </summary>
    [HttpGet("lista")]
    [ProducesResponseType<List<MenuItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MenuItemDto>>> ObtenerLista(CancellationToken ct)
    {
        var lista = await db.Menus.AsNoTracking()
            .OrderBy(m => m.IdMenuPadre.HasValue)
            .ThenBy(m => m.IdMenuPadre)
            .ThenBy(m => m.Orden)
            .ThenBy(m => m.Nombre)
            .Select(m => new MenuItemDto
            {
                IdMenu = m.IdMenu,
                Nombre = m.Nombre,
                Ruta = m.Ruta,
                Icono = m.Icono,
                IdMenuPadre = m.IdMenuPadre,
                Orden = m.Orden,
                Estado = m.Estado
            })
            .ToListAsync(ct);

        return Ok(lista);
    }

    /// <summary>
    /// GET api/menu/{id} — Una opción del menú con sus hijos directos.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var opcion = await db.Menus.AsNoTracking()
            .Include(m => m.Hijos)
            .FirstOrDefaultAsync(m => m.IdMenu == id, ct);

        if (opcion is null)
        {
            return NotFound(new ErrorApiDto { Mensaje = $"No existe la opción de menú con id {id}." });
        }

        return Ok(new MenuItemDto
        {
            IdMenu = opcion.IdMenu,
            Nombre = opcion.Nombre,
            Ruta = opcion.Ruta,
            Icono = opcion.Icono,
            IdMenuPadre = opcion.IdMenuPadre,
            Orden = opcion.Orden,
            Estado = opcion.Estado,
            Hijos = opcion.Hijos
                .OrderBy(h => h.Orden).ThenBy(h => h.Nombre)
                .Select(h => new MenuItemDto
                {
                    IdMenu = h.IdMenu,
                    Nombre = h.Nombre,
                    Ruta = h.Ruta,
                    Icono = h.Icono,
                    IdMenuPadre = h.IdMenuPadre,
                    Orden = h.Orden,
                    Estado = h.Estado
                }).ToList()
        });
    }
}
