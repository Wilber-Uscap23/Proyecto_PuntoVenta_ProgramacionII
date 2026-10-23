using Microsoft.Extensions.Configuration;

using Microsoft.AspNetCore.Components.Forms;
using WebProductos.Models;

namespace WebProductos.Services;

public class ProductosService(ApiCliente api, IConfiguration configuracion)
{
    public Task<ResultadoApi<List<ProductoDto>>> ListarAsync(string? buscar = null, int idMarca = 0) =>
        api.GetAsync<List<ProductoDto>>(ApiCliente.ConConsulta("api/productos",
            ("buscar", buscar),
            ("idMarca", idMarca > 0 ? idMarca.ToString() : null)));

    public Task<ResultadoApi<ProductoDto>> GuardarAsync(ProductoDto producto) =>
        producto.IdProducto == 0
            ? api.EnviarJsonAsync<ProductoDto>(HttpMethod.Post, "api/productos", producto)
            : api.EnviarJsonAsync<ProductoDto>(HttpMethod.Put, $"api/productos/{producto.IdProducto}", producto);

    public Task<ResultadoApi> EliminarAsync(int id) =>
        api.EliminarAsync($"api/productos/{id}");

    public Task<ResultadoApi<ProductoDto>> SubirImagenAsync(int idProducto, IBrowserFile archivo) =>
        api.SubirArchivoAsync<ProductoDto>($"api/productos/{idProducto}/imagen", archivo);

    /// <summary>
    /// La base de datos guarda solo la ruta (/images/productos/xxx.jpg). El navegador necesita la
    /// dirección completa del servidor de la API, por eso se antepone Api:UrlPublica.
    /// </summary>
    public string? UrlImagen(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return null;
        var baseUrl = (configuracion["Api:UrlPublica"] ?? string.Empty).TrimEnd('/');
        return baseUrl + ruta;
    }
}
