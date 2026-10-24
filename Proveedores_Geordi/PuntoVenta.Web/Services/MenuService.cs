// Módulo: Menú dinámico — Rodrigo Rodríguez
using PuntoVenta.Shared.Dtos.Menu;
using PuntoVenta.Web.Services.Base;

namespace PuntoVenta.Web.Services;

/// <summary>Obtiene de la API las opciones del menú lateral.</summary>
public class MenuService(IHttpClientFactory httpClientFactory, SesionService sesion)
    : ApiClienteBase(httpClientFactory, sesion)
{
    public Task<List<MenuItemDto>> ObtenerMenuAsync(CancellationToken ct = default)
        => GetAsync<List<MenuItemDto>>("api/menu", ct);
}
