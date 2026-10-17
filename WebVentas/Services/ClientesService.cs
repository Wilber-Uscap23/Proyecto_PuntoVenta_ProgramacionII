using System.Net.Http.Json;
using WebVentas.Models;

namespace WebVentas.Services
{
    public class ClientesService
    {
        private readonly HttpClient _http;
        public ClientesService(HttpClient http) => _http = http;

        public async Task<List<Cliente>> ListarAsync(string? buscar = null, bool? estado = null)
        {
            var parametros = new List<string>();
            if (!string.IsNullOrWhiteSpace(buscar))
                parametros.Add("buscar=" + Uri.EscapeDataString(buscar.Trim()));
            if (estado != null)
                parametros.Add("estado=" + estado.Value.ToString().ToLower());

            var url = "api/clientes" + (parametros.Count > 0 ? "?" + string.Join("&", parametros) : "");
            return await _http.GetFromJsonAsync<List<Cliente>>(url) ?? new();
        }

        public async Task<Cliente?> ObtenerAsync(int id)
            => await _http.GetFromJsonAsync<Cliente>($"api/clientes/{id}");

        // Cada método devuelve null si todo salió bien, o el mensaje de error.
        public async Task<string?> CrearAsync(Cliente c)
            => await ApiHelper.LeerError(await _http.PostAsJsonAsync("api/clientes", c));

        public async Task<string?> EditarAsync(Cliente c)
            => await ApiHelper.LeerError(await _http.PutAsJsonAsync($"api/clientes/{c.id_cliente}", c));

        public async Task<string?> EliminarAsync(int id)
            => await ApiHelper.LeerError(await _http.DeleteAsync($"api/clientes/{id}"));
    }
}