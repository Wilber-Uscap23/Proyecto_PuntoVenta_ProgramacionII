// Módulo: Menú dinámico — Rodrigo Rodríguez
namespace PuntoVenta.Shared.Dtos.Menu;

/// <summary>
/// Una opción del menú tal como la consume Blazor.
/// Las opciones "padre" (agrupadoras) traen Ruta = null y sus opciones en <see cref="Hijos"/>.
/// </summary>
public class MenuItemDto
{
    public int IdMenu { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Ruta { get; set; }
    public string? Icono { get; set; }
    public int? IdMenuPadre { get; set; }
    public int Orden { get; set; }
    public bool Estado { get; set; }
    public List<MenuItemDto> Hijos { get; set; } = new();
}
