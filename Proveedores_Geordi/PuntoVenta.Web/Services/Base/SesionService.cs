// =====================================================================
//  BASE DE INTEGRACIÓN — el manejo de la sesión/JWT en Blazor es de Wilber.
//
//  Los servicios de Rodrigo (MenuService y ReporteService) solo usan:
//      Task<string?> ObtenerTokenAsync()
//      event Action? SesionCambio
//  Si el servicio de Wilber tiene otro nombre, cambien el tipo en el
//  constructor de MenuService/ReporteService/NavMenu (3 líneas).
// =====================================================================
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using PuntoVenta.Shared.Dtos.Auth;

namespace PuntoVenta.Web.Services.Base;

public class SesionService(ProtectedSessionStorage storage)
{
    private const string Clave = "sesion_puntoventa";
    private LoginResponseDto? _sesion;
    private bool _cargada;

    /// <summary>Se dispara cuando el usuario inicia o cierra sesión.</summary>
    public event Action? SesionCambio;

    public string? Usuario => _sesion?.Usuario;
    public string? NombreEmpleado => _sesion?.NombreEmpleado;

    public async Task<bool> EstaAutenticadoAsync() => await ObtenerTokenAsync() is not null;

    public async Task<string?> ObtenerTokenAsync()
    {
        await CargarAsync();
        if (_sesion is null || _sesion.Expira <= DateTime.UtcNow)
        {
            return null;
        }
        return _sesion.Token;
    }

    public async Task IniciarAsync(LoginResponseDto sesion)
    {
        _sesion = sesion;
        _cargada = true;
        await storage.SetAsync(Clave, sesion);
        SesionCambio?.Invoke();
    }

    public async Task CerrarAsync()
    {
        _sesion = null;
        _cargada = true;
        await storage.DeleteAsync(Clave);
        SesionCambio?.Invoke();
    }

    private async Task CargarAsync()
    {
        if (_cargada)
        {
            return;
        }
        try
        {
            var resultado = await storage.GetAsync<LoginResponseDto>(Clave);
            _sesion = resultado.Success ? resultado.Value : null;
        }
        catch
        {
            _sesion = null; // datos dañados o de otra versión
        }
        _cargada = true;
    }
}
