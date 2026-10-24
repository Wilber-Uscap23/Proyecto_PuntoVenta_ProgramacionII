// Módulo: Proveedores y Compras
using PuntoVenta.Shared.Dtos.Proveedores;
using PuntoVenta.Web.Services.Base;

namespace PuntoVenta.Web.Services;

/// <summary>Llama a los endpoints api/proveedores de la API.</summary>
public class ProveedorService(IHttpClientFactory httpClientFactory, SesionService sesion)
    : ApiClienteEscrituraBase(httpClientFactory, sesion)
{
    public Task<List<ProveedorDto>> ListarAsync(string? buscar, string estado = "todos", CancellationToken ct = default)
        => GetAsync<List<ProveedorDto>>(ConstruirUrl("api/proveedores", ("buscar", buscar), ("estado", estado)), ct);

    public Task<ProveedorDto> CrearAsync(ProveedorGuardarDto dto, CancellationToken ct = default)
        => EnviarAsync<ProveedorDto>(HttpMethod.Post, "api/proveedores", dto, ct);

    public Task<ProveedorDto> ActualizarAsync(int id, ProveedorGuardarDto dto, CancellationToken ct = default)
        => EnviarAsync<ProveedorDto>(HttpMethod.Put, $"api/proveedores/{id}", dto, ct);

    public Task EliminarAsync(int id, CancellationToken ct = default)
        => EnviarSinRespuestaAsync(HttpMethod.Delete, $"api/proveedores/{id}", ct);
}
