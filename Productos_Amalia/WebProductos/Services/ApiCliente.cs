using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace WebProductos.Services;

public class ResultadoApi
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
}

public class ResultadoApi<T> : ResultadoApi
{
    public T? Datos { get; init; }
}

/// <summary>
/// Llama a la API REST enviando el token JWT y convierte cualquier error
/// (validación, duplicados, sin conexión, sesión vencida) en un mensaje legible.
/// </summary>
public class ApiCliente(IHttpClientFactory fabrica, ITokenProvider tokens)
{
    public const string NombreCliente = "Api";
    public const long TamanoMaximoImagen = 5 * 1024 * 1024;

    public static string ConConsulta(string url, params (string Clave, string? Valor)[] parametros)
    {
        var partes = parametros
            .Where(p => !string.IsNullOrWhiteSpace(p.Valor))
            .Select(p => $"{p.Clave}={Uri.EscapeDataString(p.Valor!)}")
            .ToList();
        return partes.Count == 0 ? url : $"{url}?{string.Join("&", partes)}";
    }

    public Task<ResultadoApi<T>> GetAsync<T>(string url) =>
        EnviarAsync<T>(HttpMethod.Get, url);

    public Task<ResultadoApi<T>> EnviarJsonAsync<T>(HttpMethod metodo, string url, object cuerpo) =>
        EnviarAsync<T>(metodo, url, JsonContent.Create(cuerpo));

    public async Task<ResultadoApi> EliminarAsync(string url)
    {
        var (respuesta, error) = await EjecutarAsync(HttpMethod.Delete, url, null);
        respuesta?.Dispose();
        return new ResultadoApi { Ok = respuesta is not null, Error = error };
    }

    public async Task<ResultadoApi<T>> SubirArchivoAsync<T>(string url, IBrowserFile archivo)
    {
        try
        {
            using var contenido = new MultipartFormDataContent();
            var tipo = string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType;
            var flujo = new StreamContent(archivo.OpenReadStream(TamanoMaximoImagen));
            flujo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
            contenido.Add(flujo, "archivo", archivo.Name);
            return await EnviarAsync<T>(HttpMethod.Post, url, contenido);
        }
        catch (IOException)
        {
            return new ResultadoApi<T> { Ok = false, Error = "La imagen no puede pesar más de 5 MB." };
        }
    }

    private async Task<ResultadoApi<T>> EnviarAsync<T>(HttpMethod metodo, string url, HttpContent? contenido = null)
    {
        var (respuesta, error) = await EjecutarAsync(metodo, url, contenido);
        if (respuesta is null)
            return new ResultadoApi<T> { Ok = false, Error = error };

        using (respuesta)
        {
            try
            {
                var datos = await respuesta.Content.ReadFromJsonAsync<T>();
                return new ResultadoApi<T> { Ok = true, Datos = datos };
            }
            catch (JsonException)
            {
                return new ResultadoApi<T> { Ok = false, Error = "La API devolvió una respuesta inválida." };
            }
        }
    }

    private async Task<(HttpResponseMessage? Respuesta, string? Error)> EjecutarAsync(
        HttpMethod metodo, string url, HttpContent? contenido)
    {
        try
        {
            using var solicitud = new HttpRequestMessage(metodo, url) { Content = contenido };

            var token = await tokens.ObtenerTokenAsync();
            if (!string.IsNullOrEmpty(token))
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var respuesta = await fabrica.CreateClient(NombreCliente).SendAsync(solicitud);
            if (respuesta.IsSuccessStatusCode)
                return (respuesta, null);

            var error = await LeerErrorAsync(respuesta);
            respuesta.Dispose();
            return (null, error);
        }
        catch (HttpRequestException)
        {
            return (null, "No se pudo conectar con la API. Verifique que esté publicada y en ejecución.");
        }
        catch (TaskCanceledException)
        {
            return (null, "La API tardó demasiado en responder.");
        }
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            return "Su sesión expiró o no está autorizado. Inicie sesión nuevamente.";
        if (respuesta.StatusCode == HttpStatusCode.Forbidden)
            return "No tiene permiso para realizar esta acción.";

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            var raiz = doc.RootElement;
            if (raiz.ValueKind == JsonValueKind.Object)
            {
                // { "mensaje": "..." }  (errores de negocio de nuestra API)
                if (raiz.TryGetProperty("mensaje", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString()!;

                // { "errors": { "Campo": ["..."] } }  (validación automática de ASP.NET Core)
                if (raiz.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                {
                    var mensajes = e.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Select(x => x.GetString())
                        .Where(s => !string.IsNullOrEmpty(s));
                    var texto = string.Join(" ", mensajes);
                    if (texto.Length > 0) return texto;
                }
            }
        }
        catch (JsonException) { /* el cuerpo no era JSON */ }

        return $"La API respondió con un error ({(int)respuesta.StatusCode}).";
    }
}
