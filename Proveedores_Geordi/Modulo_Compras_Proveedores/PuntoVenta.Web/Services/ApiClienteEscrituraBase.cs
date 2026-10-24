// Módulo: Proveedores y Compras
// Extiende ApiClienteBase (que solo tiene GET) con POST, PUT y DELETE.
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoVenta.Web.Services.Base;

namespace PuntoVenta.Web.Services;

public abstract class ApiClienteEscrituraBase(IHttpClientFactory httpClientFactory, SesionService sesion)
    : ApiClienteBase(httpClientFactory, sesion)
{
    /// <summary>Envía POST/PUT con cuerpo JSON y devuelve la respuesta deserializada.</summary>
    protected async Task<T> EnviarAsync<T>(HttpMethod metodo, string url, object cuerpo, CancellationToken ct = default)
    {
        using var response = await EjecutarAsync(metodo, url, cuerpo, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new ApiException("La API devolvió una respuesta vacía.");
    }

    /// <summary>Envía una petición (por ejemplo DELETE) que no devuelve contenido.</summary>
    protected async Task EnviarSinRespuestaAsync(HttpMethod metodo, string url, CancellationToken ct = default)
    {
        using var response = await EjecutarAsync(metodo, url, null, ct);
    }

    private async Task<HttpResponseMessage> EjecutarAsync(HttpMethod metodo, string url, object? cuerpo, CancellationToken ct)
    {
        var token = await sesion.ObtenerTokenAsync()
            ?? throw new ApiException("Su sesión expiró. Inicie sesión nuevamente.", HttpStatusCode.Unauthorized);

        using var request = new HttpRequestMessage(metodo, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null)
        {
            request.Content = JsonContent.Create(cuerpo);
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory.CreateClient(NombreCliente).SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("No se pudo conectar con la API. Verifique que esté publicada y en ejecución.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("La API tardó demasiado en responder.");
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ApiException("Su sesión expiró. Inicie sesión nuevamente.", response.StatusCode),
                HttpStatusCode.Forbidden => new ApiException("No tiene permiso para realizar esta acción.", response.StatusCode),
                HttpStatusCode.NotFound => new ApiException(await LeerErrorAsync(response, "No se encontró la información solicitada."), response.StatusCode),
                HttpStatusCode.BadRequest => new ApiException(await LeerErrorAsync(response, "Los datos enviados no son válidos."), response.StatusCode),
                HttpStatusCode.Conflict => new ApiException(await LeerErrorAsync(response, "La operación entra en conflicto con datos existentes."), response.StatusCode),
                _ => new ApiException($"La API respondió con un error ({(int)response.StatusCode}).", response.StatusCode)
            };
        }
    }

    /// <summary>
    /// Lee el mensaje de error de la API: primero el formato propio (ErrorApiDto.Mensaje)
    /// y, si no, el de validación automática de ASP.NET (campo "errors").
    /// </summary>
    private static async Task<string> LeerErrorAsync(HttpResponseMessage response, string porDefecto)
    {
        try
        {
            await using var flujo = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(flujo);
            var raiz = doc.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
            {
                return porDefecto;
            }

            foreach (var propiedad in raiz.EnumerateObject())
            {
                if (propiedad.NameEquals("mensaje") || propiedad.Name.Equals("mensaje", StringComparison.OrdinalIgnoreCase))
                {
                    var texto = propiedad.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(texto)) return texto;
                }
            }

            if (raiz.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
            {
                var mensajes = errores.EnumerateObject()
                    .SelectMany(e => e.Value.ValueKind == JsonValueKind.Array ? e.Value.EnumerateArray() : [])
                    .Select(m => m.GetString())
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct();
                var unido = string.Join(" ", mensajes);
                if (unido.Length > 0) return unido;
            }
        }
        catch
        {
            // Si la respuesta no es JSON se usa el mensaje por defecto.
        }
        return porDefecto;
    }

    protected static string ConstruirUrl(string ruta, params (string Nombre, object? Valor)[] parametros)
    {
        var partes = new List<string>();
        foreach (var (nombre, valor) in parametros)
        {
            var texto = valor switch
            {
                null => null,
                string s when string.IsNullOrWhiteSpace(s) => null,
                string s => s.Trim(),
                DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => valor.ToString()
            };
            if (texto is not null)
            {
                partes.Add($"{nombre}={Uri.EscapeDataString(texto)}");
            }
        }
        return partes.Count == 0 ? ruta : $"{ruta}?{string.Join("&", partes)}";
    }
}
