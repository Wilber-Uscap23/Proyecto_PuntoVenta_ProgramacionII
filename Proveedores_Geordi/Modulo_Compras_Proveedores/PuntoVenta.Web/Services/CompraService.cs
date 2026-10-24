// Módulo: Proveedores y Compras
using PuntoVenta.Shared.Dtos.Compras;
using PuntoVenta.Web.Services.Base;

namespace PuntoVenta.Web.Services;

/// <summary>Llama a los endpoints api/compras de la API.</summary>
public class CompraService(IHttpClientFactory httpClientFactory, SesionService sesion)
    : ApiClienteEscrituraBase(httpClientFactory, sesion)
{
    public Task<List<CompraListadoDto>> ListarAsync(DateTime? desde, DateTime? hasta, int? idProveedor, CancellationToken ct = default)
        => GetAsync<List<CompraListadoDto>>(ConstruirUrl("api/compras",
            ("desde", desde), ("hasta", hasta), ("idProveedor", idProveedor)), ct);

    public Task<CompraDetalleDto> ObtenerAsync(int id, CancellationToken ct = default)
        => GetAsync<CompraDetalleDto>($"api/compras/{id}", ct);

    public Task<List<ProductoCompraDto>> BuscarProductosAsync(string? buscar, CancellationToken ct = default)
        => GetAsync<List<ProductoCompraDto>>(ConstruirUrl("api/compras/productos", ("buscar", buscar)), ct);

    public Task<CompraDetalleDto> CrearAsync(CompraCrearDto dto, CancellationToken ct = default)
        => EnviarAsync<CompraDetalleDto>(HttpMethod.Post, "api/compras", dto, ct);
}
