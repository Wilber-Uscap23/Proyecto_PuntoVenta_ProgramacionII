// Módulo: Menú dinámico y Reportes — Rodrigo Rodríguez
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PuntoVenta.Shared.Dtos.Reportes;
using PuntoVenta.Web.Services.Base;

namespace PuntoVenta.Web.Services;

/// <summary>
/// Lógica común para llamar a la API con el token JWT y convertir los errores
/// en mensajes entendibles (<see cref="ApiException"/>).
/// </summary>
public abstract class ApiClienteBase(IHttpClientFactory httpClientFactory, SesionService sesion)
{
    /// <summary>Nombre del HttpClient registrado en Program.cs.</summary>
    public const string NombreCliente = "Api";

    protected async Task<T> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var token = await sesion.ObtenerTokenAsync()
            ?? throw new ApiException("Su sesión expiró. Inicie sesión nuevamente.", HttpStatusCode.Unauthorized);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            var http = httpClientFactory.CreateClient(NombreCliente);
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("No se pudo conectar con la API. Verifique que esté publicada y en ejecución.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("La API tardó demasiado en responder.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(ct)
                    ?? throw new ApiException("La API devolvió una respuesta vacía.");
            }

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ApiException("Su sesión expiró. Inicie sesión nuevamente.", response.StatusCode),
                HttpStatusCode.Forbidden => new ApiException("No tiene permiso para ver esta información.", response.StatusCode),
                HttpStatusCode.NotFound => new ApiException(await LeerMensajeAsync(response, "No se encontró la información solicitada."), response.StatusCode),
                HttpStatusCode.BadRequest => new ApiException(await LeerMensajeAsync(response, "Los filtros enviados no son válidos."), response.StatusCode),
                _ => new ApiException($"La API respondió con un error ({(int)response.StatusCode}).", response.StatusCode)
            };
        }
    }

    private static async Task<string> LeerMensajeAsync(HttpResponseMessage response, string porDefecto)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorApiDto>();
            return string.IsNullOrWhiteSpace(error?.Mensaje) ? porDefecto : error.Mensaje;
        }
        catch
        {
            return porDefecto;
        }
    }
}
