using WebProductos.Models;

namespace WebProductos.Services;

public class MarcasService(ApiCliente api)
{
    public Task<ResultadoApi<List<MarcaDto>>> ListarAsync(string? buscar = null) =>
        api.GetAsync<List<MarcaDto>>(ApiCliente.ConConsulta("api/marcas", ("buscar", buscar)));

    public Task<ResultadoApi<MarcaDto>> GuardarAsync(MarcaDto marca) =>
        marca.IdMarca == 0
            ? api.EnviarJsonAsync<MarcaDto>(HttpMethod.Post, "api/marcas", marca)
            : api.EnviarJsonAsync<MarcaDto>(HttpMethod.Put, $"api/marcas/{marca.IdMarca}", marca);

    public Task<ResultadoApi> EliminarAsync(int id) =>
        api.EliminarAsync($"api/marcas/{id}");
}
