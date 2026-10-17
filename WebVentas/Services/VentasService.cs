using System.Net.Http.Json;
using WebVentas.Models;

namespace WebVentas.Services
{
    public class VentasService
    {
        private readonly HttpClient _http;
        public VentasService(HttpClient http) => _http = http;

        public async Task<List<Venta>> ListarAsync()
            => await _http.GetFromJsonAsync<List<Venta>>("api/ventas") ?? new();

        public async Task<CatalogosVenta> CatalogosAsync(bool soloActivos = false)
            => await _http.GetFromJsonAsync<CatalogosVenta>(
                   $"api/ventas/catalogos?soloActivos={soloActivos.ToString().ToLower()}") ?? new();

        public async Task<Venta?> ObtenerAsync(int id)
            => await _http.GetFromJsonAsync<Venta>($"api/ventas/{id}");

        // Cada método devuelve null si todo salió bien, o el mensaje de error.
        public async Task<string?> CrearAsync(Venta venta)
            => await ApiHelper.LeerError(await _http.PostAsJsonAsync("api/ventas", venta));

        public async Task<string?> EditarAsync(Venta venta)
            => await ApiHelper.LeerError(await _http.PutAsJsonAsync($"api/ventas/{venta.id_venta}", venta));

        public async Task<string?> EliminarAsync(int id)
            => await ApiHelper.LeerError(await _http.DeleteAsync($"api/ventas/{id}"));

        // Emite la factura de la venta (o recupera la que ya existe, con el mismo número FEL).
        public async Task<(FacturaDocumento? factura, string? error)> EmitirFacturaAsync(int idVenta)
        {
            var respuesta = await _http.PostAsync($"api/facturas/venta/{idVenta}", null);
            if (!respuesta.IsSuccessStatusCode)
                return (null, await ApiHelper.LeerError(respuesta));

            var factura = await respuesta.Content.ReadFromJsonAsync<FacturaDocumento>();
            return (factura, null);
        }
    }
}