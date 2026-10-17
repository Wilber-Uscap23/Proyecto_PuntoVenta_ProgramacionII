using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebVentas.Models;

namespace WebVentas.Services
{
    // Datos que se piden al registrar un cliente nuevo desde la pantalla de venta.
    public class NuevoCliente
    {
        public string nombre { get; set; } = "";
        public string apellido { get; set; } = "";
        public string? nit { get; set; }
        public string? telefono { get; set; }
        public string? correo { get; set; }
        public string? direccion { get; set; }
    }

    public class BusquedaService
    {
        private readonly HttpClient _http;
        public BusquedaService(HttpClient http) => _http = http;

        private const string ErrorConexion = "No se pudo conectar con la API. Verifica que ApiVentas esté ejecutándose.";

        // ---------- Cliente por NIT ----------
        // (Cliente, null)  -> encontrado
        // (null, null)     -> no está registrado
        // (null, "texto")  -> error (NIT inválido, cliente inactivo, API caída...)
        public async Task<(OpcionCliente? Cliente, string? Error)> BuscarClienteAsync(string nit)
        {
            try
            {
                var resp = await _http.GetAsync($"api/busqueda/cliente/{Uri.EscapeDataString(nit.Trim())}");
                if (resp.StatusCode == HttpStatusCode.NotFound) return (null, await ErrorSiEndpointNoExiste(resp));
                if (resp.IsSuccessStatusCode)
                    return (await resp.Content.ReadFromJsonAsync<OpcionCliente>(), null);
                return (null, await LeerError(resp));
            }
            catch (HttpRequestException)
            {
                return (null, ErrorConexion);
            }
        }

        // Para el modo "Editar venta": trae el cliente ya guardado en la venta.
        public async Task<OpcionCliente?> ObtenerClienteAsync(int id)
        {
            try
            {
                var resp = await _http.GetAsync($"api/clientes/{id}");
                return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<OpcionCliente>() : null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // ---------- Registrar cliente (usa el POST api/clientes que ya existe) ----------
        public async Task<(OpcionCliente? Cliente, string? Error)> RegistrarClienteAsync(NuevoCliente c)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("api/clientes", c);
                if (resp.IsSuccessStatusCode)
                    return (await resp.Content.ReadFromJsonAsync<OpcionCliente>(), null);
                return (null, await LeerError(resp));
            }
            catch (HttpRequestException)
            {
                return (null, ErrorConexion);
            }
        }

        // Mismas reglas que Models/cliente.cs de la API (la API las vuelve a validar).
        public static List<string> ValidarNuevoCliente(NuevoCliente c)
        {
            var errores = new List<string>();
            const string patronNombre = @"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ][A-Za-zÁÉÍÓÚÜÑáéíóúüñ'’ .-]*$";

            var nombre = (c.nombre ?? "").Trim();
            var apellido = (c.apellido ?? "").Trim();

            if (nombre.Length == 0) errores.Add("El nombre es obligatorio.");
            else if (nombre.Length < 2 || nombre.Length > 100) errores.Add("El nombre debe tener entre 2 y 100 caracteres.");
            else if (!Regex.IsMatch(nombre, patronNombre)) errores.Add("El nombre solo puede contener letras y espacios.");

            if (apellido.Length == 0) errores.Add("El apellido es obligatorio.");
            else if (apellido.Length < 2 || apellido.Length > 100) errores.Add("El apellido debe tener entre 2 y 100 caracteres.");
            else if (!Regex.IsMatch(apellido, patronNombre)) errores.Add("El apellido solo puede contener letras y espacios.");

            if (!string.IsNullOrWhiteSpace(c.telefono) &&
                !Regex.IsMatch(c.telefono.Trim(), @"^(\+?502[ -]?)?[2-7]\d{3}[ -]?\d{4}$"))
                errores.Add("Teléfono inválido. Usa 8 dígitos que empiecen con 2 a 7 (ej. 5551 2345).");

            if (!string.IsNullOrWhiteSpace(c.correo))
            {
                var correo = c.correo.Trim();
                if (correo.Length > 150) errores.Add("El correo no puede pasar de 150 caracteres.");
                else if (!Regex.IsMatch(correo, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$"))
                    errores.Add("El correo no tiene un formato válido (ej. nombre@correo.com).");
            }

            if (!string.IsNullOrWhiteSpace(c.direccion) && c.direccion.Trim().Length > 255)
                errores.Add("La dirección no puede pasar de 255 caracteres.");

            return errores;
        }

        // ---------- Empleados por id o nombre ----------
        public async Task<(List<OpcionEmpleado> Lista, string? Error)> BuscarEmpleadosAsync(string texto, bool soloActivos = true)
        {
            try
            {
                var url = $"api/busqueda/empleados?q={Uri.EscapeDataString(texto.Trim())}&soloActivos={soloActivos.ToString().ToLower()}";
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return (new(), await LeerError(resp));
                var lista = await resp.Content.ReadFromJsonAsync<List<OpcionEmpleado>>() ?? new();
                return (lista, null);
            }
            catch (HttpRequestException)
            {
                return (new(), ErrorConexion);
            }
        }

        // ---------- Producto por código de barras (o id) ----------
        // (Producto, null)  -> encontrado
        // (null, null)      -> no existe
        // (null, "texto")   -> error (inactivo, API caída, endpoint inexistente...)
        public async Task<(OpcionProducto? Producto, string? Error)> BuscarProductoAsync(string codigo)
        {
            try
            {
                var resp = await _http.GetAsync($"api/busqueda/producto?codigo={Uri.EscapeDataString(codigo.Trim())}");
                if (resp.StatusCode == HttpStatusCode.NotFound) return (null, await ErrorSiEndpointNoExiste(resp));
                if (resp.IsSuccessStatusCode)
                    return (await resp.Content.ReadFromJsonAsync<OpcionProducto>(), null);
                return (null, await LeerError(resp));
            }
            catch (HttpRequestException)
            {
                return (null, ErrorConexion);
            }
        }

        // Para el modo "Editar venta": trae los datos de los productos que ya tiene la venta.
        public async Task<List<OpcionProducto>> ProductosPorIdsAsync(IEnumerable<int> ids)
        {
            try
            {
                var lista = string.Join(",", ids.Distinct());
                if (lista.Length == 0) return new();
                var resp = await _http.GetAsync($"api/busqueda/productos?ids={lista}");
                return resp.IsSuccessStatusCode
                    ? await resp.Content.ReadFromJsonAsync<List<OpcionProducto>>() ?? new()
                    : new();
            }
            catch (HttpRequestException)
            {
                return new();
            }
        }

        // Un 404 con {"mensaje": ...} es la respuesta de la API "no existe" (devuelve null = no encontrado).
        // Un 404 sin ese cuerpo significa que la API no tiene el endpoint (está corriendo con código viejo).
        private static async Task<string?> ErrorSiEndpointNoExiste(HttpResponseMessage resp)
        {
            var cuerpo = await resp.Content.ReadAsStringAsync();
            return cuerpo.Contains("mensaje")
                ? null
                : "La API no tiene este endpoint. Reinicia ApiVentas con los archivos nuevos.";
        }

        // Lee el mensaje de error de la API: {"mensaje": "..."} o el formato de validación de ASP.NET.
        private static async Task<string> LeerError(HttpResponseMessage resp)
        {
            try
            {
                var texto = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(texto);
                var raiz = doc.RootElement;

                if (raiz.TryGetProperty("mensaje", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString()!;

                if (raiz.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                {
                    var mensajes = errs.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(x => x.GetString() ?? ""))
                        .Where(s => s.Length > 0);
                    var unidos = string.Join(" ", mensajes);
                    if (unidos.Length > 0) return unidos;
                }
            }
            catch
            {
                // si el cuerpo no es JSON, caemos al mensaje genérico
            }
            return $"Error del servidor ({(int)resp.StatusCode}).";
        }
    }
}