using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ComprasApp.Client.Services;

// Contenedor mínimo del JWT. El módulo de Login debe llamar a TokenStore.Token = ... tras autenticar
// (y puede persistirlo en localStorage si desea sobrevivir a recargas).
public class TokenStore
{
    public string? Token { get; set; }
}

// Adjunta "Authorization: Bearer <jwt>" a cada petición hacia la API.
public class AuthHandler(TokenStore store) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(store.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", store.Token);
        return base.SendAsync(request, ct);
    }
}

public static class ApiErrors
{
    public static async Task<string> ReadAsync(HttpResponseMessage r)
    {
        if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return "Sesión no válida o expirada. Inicie sesión nuevamente.";
        try
        {
            var doc = await r.Content.ReadFromJsonAsync<JsonElement>();
            if (doc.TryGetProperty("message", out var m)) return m.GetString() ?? "Error.";
            if (doc.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                return string.Join(" | ", e.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(x => x.GetString())));
        }
        catch { /* cuerpo vacío o no JSON */ }
        return $"Error {(int)r.StatusCode} al comunicarse con el servidor.";
    }
}
