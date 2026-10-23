using Microsoft.Extensions.Configuration;

namespace WebProductos.Services;

/// <summary>
/// Entrega el token JWT que se enviará a la API en el encabezado Authorization.
/// Así este módulo no depende de cómo Wilber guarde la sesión.
/// </summary>
public interface ITokenProvider
{
    Task<string?> ObtenerTokenAsync();
}

/// <summary>
/// Versión para probar el módulo solo: toma el token de appsettings ("Api:TokenDePrueba").
/// Si está vacío no se envía encabezado (sirve con Seguridad:ExigirJwt = false en la API).
/// </summary>
public class TokenProviderConfiguracion(IConfiguration configuracion) : ITokenProvider
{
    public Task<string?> ObtenerTokenAsync()
    {
        var token = configuracion["Api:TokenDePrueba"];
        return Task.FromResult(string.IsNullOrWhiteSpace(token) ? null : token);
    }
}
