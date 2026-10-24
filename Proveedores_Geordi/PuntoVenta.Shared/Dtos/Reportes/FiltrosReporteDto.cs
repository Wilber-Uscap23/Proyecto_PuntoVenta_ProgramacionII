// Módulo: Reportes — Rodrigo Rodríguez
namespace PuntoVenta.Shared.Dtos.Reportes;

/// <summary>Elemento simple para llenar los combos de filtros de los reportes.</summary>
public class OpcionFiltroDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>Listas para los combos de filtros (marcas, proveedores, clientes y empleados).</summary>
public class FiltrosReporteDto
{
    public List<OpcionFiltroDto> Marcas { get; set; } = new();
    public List<OpcionFiltroDto> Proveedores { get; set; } = new();
    public List<OpcionFiltroDto> Clientes { get; set; } = new();
    public List<OpcionFiltroDto> Empleados { get; set; } = new();
}

/// <summary>Formato de error que devuelve la API: { "mensaje": "..." }.</summary>
public class ErrorApiDto
{
    public string Mensaje { get; set; } = string.Empty;
}
