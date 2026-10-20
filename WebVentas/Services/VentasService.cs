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

        // ---------- Búsquedas del formulario de venta ----------
        // Cada método devuelve el error (mensaje) en null si todo salió bien.

        // Cliente por NIT. Si el NIT viene vacío, la API busca "CF".
        public async Task<(OpcionCliente? cliente, string? error)> BuscarClienteAsync(string? nit)
        {
            var respuesta = await _http.GetAsync($"api/ventas/buscar-cliente?nit={Uri.EscapeDataString(nit ?? "")}");
            if (!respuesta.IsSuccessStatusCode)
                return (null, await ApiHelper.LeerError(respuesta));

            return (await respuesta.Content.ReadFromJsonAsync<OpcionCliente>(), null);
        }

        // Empleados por id o parte del nombre.
        public async Task<(List<OpcionEmpleado> empleados, string? error)> BuscarEmpleadosAsync(string texto)
        {
            var respuesta = await _http.GetAsync($"api/ventas/buscar-empleados?texto={Uri.EscapeDataString(texto)}");
            if (!respuesta.IsSuccessStatusCode)
                return (new List<OpcionEmpleado>(), await ApiHelper.LeerError(respuesta));

            var lista = await respuesta.Content.ReadFromJsonAsync<List<OpcionEmpleado>>() ?? new();
            return (lista, null);
        }

        // Producto por código de barras / código o por id.
        public async Task<(OpcionProducto? producto, string? error)> BuscarProductoAsync(string codigo)
        {
            var respuesta = await _http.GetAsync($"api/ventas/buscar-producto?codigo={Uri.EscapeDataString(codigo)}");
            if (!respuesta.IsSuccessStatusCode)
                return (null, await ApiHelper.LeerError(respuesta));

            return (await respuesta.Content.ReadFromJsonAsync<OpcionProducto>(), null);
        }

        // ---------- Venta y factura ----------

        // Vista previa de la factura: valida la venta pero NO guarda nada.
        public async Task<(FacturaDocumento? factura, string? error)> VistaPreviaAsync(Venta venta)
        {
            var respuesta = await _http.PostAsJsonAsync("api/ventas/vista-previa", venta);
            if (!respuesta.IsSuccessStatusCode)
                return (null, await ApiHelper.LeerError(respuesta));

            return (await respuesta.Content.ReadFromJsonAsync<FacturaDocumento>(), null);
        }

        // Confirma la venta (se guarda la venta y su factura) y devuelve el id de la venta creada.
        public async Task<(int? idVenta, string? error)> CrearVentaAsync(Venta venta)
        {
            var respuesta = await _http.PostAsJsonAsync("api/ventas", venta);
            if (!respuesta.IsSuccessStatusCode)
                return (null, await ApiHelper.LeerError(respuesta));

            var creada = await respuesta.Content.ReadFromJsonAsync<Venta>();
            return (creada?.id_venta, null);
        }

        public async Task<string?> CrearAsync(Venta venta)
            => await ApiHelper.LeerError(await _http.PostAsJsonAsync("api/ventas", venta));

        public async Task<string?> EditarAsync(Venta venta)
            => await ApiHelper.LeerError(await _http.PutAsJsonAsync($"api/ventas/{venta.id_venta}", venta));

        public async Task<string?> EliminarAsync(int id)
            => await ApiHelper.LeerError(await _http.DeleteAsync($"api/ventas/{id}"));

        // Devuelve la factura de la venta (o la emite si es una venta anterior sin factura).
        public async Task<(FacturaDocumento? factura, string? error)> EmitirFacturaAsync(int idVenta)
        {
            var respuesta = await _http.PostAsync($"api/facturas/venta/{idVenta}", null);
            if (!respuesta.IsSuccessStatusCode)
                return (null, await ApiHelper.LeerError(respuesta));

            return (await respuesta.Content.ReadFromJsonAsync<FacturaDocumento>(), null);
        }
    }
}