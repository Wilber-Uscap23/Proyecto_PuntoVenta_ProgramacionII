// Módulo: Menú dinámico — Rodrigo Rodríguez
using PuntoVenta.API.Models;
using PuntoVenta.Shared.Dtos.Menu;

namespace PuntoVenta.API.Services;

/// <summary>
/// Convierte la lista plana de la tabla `menu` en un árbol padre → hijos.
/// </summary>
public static class MenuArbol
{
    /// <summary>
    /// Arma el árbol del menú.
    /// <list type="bullet">
    /// <item>Solo toma en cuenta las opciones con estado = 1 (si se oculta un padre, se ocultan sus hijos).</item>
    /// <item>Ordena cada nivel por <c>orden</c> y luego por <c>nombre</c>.</item>
    /// <item>Quita las opciones agrupadoras (sin ruta) que se quedan sin hijos visibles.</item>
    /// <item>Ignora registros con padre inexistente o con ciclos (A → B → A) para no romper el menú.</item>
    /// </list>
    /// </summary>
    public static List<MenuItemDto> Construir(IEnumerable<Menu> opciones)
    {
        var activas = opciones.Where(o => o.Estado).ToList();
        var porPadre = activas
            .Where(o => o.IdMenuPadre.HasValue)
            .ToLookup(o => o.IdMenuPadre!.Value);

        var visitados = new HashSet<int>();

        List<MenuItemDto> ConstruirNivel(IEnumerable<Menu> nivel)
        {
            var resultado = new List<MenuItemDto>();
            foreach (var opcion in nivel.OrderBy(o => o.Orden).ThenBy(o => o.Nombre))
            {
                if (!visitados.Add(opcion.IdMenu))
                {
                    continue; // ya se procesó: evita ciclos
                }

                var item = new MenuItemDto
                {
                    IdMenu = opcion.IdMenu,
                    Nombre = opcion.Nombre,
                    Ruta = string.IsNullOrWhiteSpace(opcion.Ruta) ? null : opcion.Ruta.Trim(),
                    Icono = string.IsNullOrWhiteSpace(opcion.Icono) ? null : opcion.Icono.Trim(),
                    IdMenuPadre = opcion.IdMenuPadre,
                    Orden = opcion.Orden,
                    Estado = opcion.Estado,
                    Hijos = ConstruirNivel(porPadre[opcion.IdMenu])
                };

                // Una opción sin ruta solo sirve para agrupar: si no tiene hijos, no se muestra.
                if (item.Ruta is null && item.Hijos.Count == 0)
                {
                    continue;
                }

                resultado.Add(item);
            }
            return resultado;
        }

        var raices = activas.Where(o => o.IdMenuPadre is null);
        return ConstruirNivel(raices);
    }
}
