using System.Text.Json;

namespace WebVentas.Services
{
    public static class ApiHelper
    {
        // Devuelve null si la respuesta fue exitosa; si no, el mensaje de error de la API.
        public static async Task<string?> LeerError(HttpResponseMessage r)
        {
            if (r.IsSuccessStatusCode) return null;

            var cuerpo = await r.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(cuerpo);
                var raiz = doc.RootElement;
                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    if (raiz.TryGetProperty("mensaje", out var m) && m.ValueKind == JsonValueKind.String)
                        return m.GetString();

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
            catch (JsonException) { }

            var linea = cuerpo.Split('\n')[0];
            if (linea.Length > 300) linea = linea[..300];
            return $"Error {(int)r.StatusCode}: {linea}";
        }
    }
}