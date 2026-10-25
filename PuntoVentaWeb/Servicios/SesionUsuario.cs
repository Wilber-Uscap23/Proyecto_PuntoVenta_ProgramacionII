using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PuntoVentaWeb.Servicios;

// ============================================
// DATOS DE LA SESIÓN
// ============================================
public class DatosSesion
{
    public string Token { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Rol { get; set; } = "";
    public int IdUsuario { get; set; }
    public int IdEmpleado { get; set; }
    public DateTime ExpiracionUtc { get; set; }
}

// ============================================
// OPCIONES DEL MENÚ DINÁMICO
// ============================================
public class OpcionMenuDinamica
{
    public int IdMenu { get; set; }

    public string Nombre { get; set; } = "";

    public string Ruta { get; set; } = "";

    public string Icono { get; set; } = "";

    public int? IdMenuPadre { get; set; }

    public int Orden { get; set; }
}

// ============================================
// SERVICIO DE SESIÓN DEL USUARIO
// ============================================
public class SesionUsuario
{
    private const string ClaveAlmacenamiento =
        "PuntoVenta.Sesion";

    private readonly ProtectedSessionStorage _almacen;
    private readonly HttpClient _http;

    private DatosSesion? _datos;

    // ========================================
    // ALMACENAMIENTO DEL MENÚ DINÁMICO
    // ========================================
    private List<OpcionMenuDinamica> _opcionesMenu = new();

    public IReadOnlyList<OpcionMenuDinamica> OpcionesMenu =>
        EstaAutenticado
            ? _opcionesMenu
            : Array.Empty<OpcionMenuDinamica>();

    public bool MenuCargado { get; private set; }

    public string ErrorMenu { get; private set; } = "";

    // ========================================
    // CONSTRUCTOR
    // ========================================
    public SesionUsuario(
        ProtectedSessionStorage almacen,
        HttpClient http)
    {
        _almacen = almacen;
        _http = http;
    }

    // ========================================
    // ESTADO DE LA SESIÓN
    // ========================================
    public bool Inicializada { get; private set; }

    public bool EstaAutenticado =>
        _datos != null &&
        !string.IsNullOrWhiteSpace(_datos.Token) &&
        _datos.ExpiracionUtc > DateTime.UtcNow;

    public string Usuario =>
        EstaAutenticado ? _datos!.Usuario : "";

    public string NombreCompleto =>
        EstaAutenticado ? _datos!.NombreCompleto : "";

    public string Rol =>
        EstaAutenticado ? _datos!.Rol : "";

    // ========================================
    // RECUPERAR SESIÓN AL ABRIR LA PÁGINA
    // ========================================
    public async Task RestaurarAsync()
    {
        if (Inicializada)
            return;

        try
        {
            var resultado =
                await _almacen.GetAsync<DatosSesion>(
                    ClaveAlmacenamiento);

            if (resultado.Success &&
                resultado.Value != null &&
                !string.IsNullOrWhiteSpace(resultado.Value.Token) &&
                resultado.Value.ExpiracionUtc > DateTime.UtcNow)
            {
                _datos = resultado.Value;

                // Recuperar el token JWT.
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        _datos.Token);

                // Recuperar las opciones del menú
                // correspondientes al rol del usuario.
                await CargarMenuAsync();
            }
            else
            {
                LimpiarMemoria();

                await _almacen.DeleteAsync(
                    ClaveAlmacenamiento);
            }
        }
        catch (Exception)
        {
            // Si la sesión guardada expiró, fue
            // alterada o no puede recuperarse,
            // exigimos iniciar sesión nuevamente.
            LimpiarMemoria();
        }
        finally
        {
            Inicializada = true;
        }
    }

    // ========================================
    // GUARDAR NUEVO INICIO DE SESIÓN
    // ========================================
    public async Task IniciarAsync(DatosSesion datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Token) ||
            datos.ExpiracionUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "El token de acceso no es válido.");
        }

        // Guardar primero la sesión protegida.
        await _almacen.SetAsync(
            ClaveAlmacenamiento,
            datos);

        _datos = datos;
        Inicializada = true;

        // Las consultas posteriores a la API
        // enviarán automáticamente el JWT.
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                datos.Token);

        // Consultar el menú correspondiente
        // al usuario que inició sesión.
        await CargarMenuAsync();
    }

    // ========================================
    // CARGAR MENÚ DINÁMICO DESDE LA API
    // ========================================
    public async Task CargarMenuAsync()
    {
        // Limpiar opciones anteriores.
        _opcionesMenu = new List<OpcionMenuDinamica>();

        MenuCargado = false;
        ErrorMenu = "";

        if (!EstaAutenticado)
        {
            ErrorMenu = "No existe una sesión activa.";
            return;
        }

        try
        {
            // El controlador identifica el rol
            // directamente desde el token JWT.
            using var respuesta = await _http.GetAsync(
                "api/menu/mis-opciones");

            if (!respuesta.IsSuccessStatusCode)
            {
                ErrorMenu =
                    $"No se pudo cargar el menú. " +
                    $"Código HTTP: {(int)respuesta.StatusCode}.";

                return;
            }

            // Convertir la respuesta de la API
            // en una lista de opciones.
            var opciones = await respuesta.Content
                .ReadFromJsonAsync<List<OpcionMenuDinamica>>();

            _opcionesMenu =
                opciones ?? new List<OpcionMenuDinamica>();

            // Ordenar el menú.
            _opcionesMenu = _opcionesMenu
                .OrderBy(opcion => opcion.Orden)
                .ThenBy(opcion => opcion.IdMenu)
                .ToList();

            MenuCargado = true;
        }
        catch (HttpRequestException)
        {
            ErrorMenu =
                "No fue posible conectarse con la API " +
                "para consultar el menú.";
        }
        catch (TaskCanceledException)
        {
            ErrorMenu =
                "La consulta del menú tardó demasiado.";
        }
        catch (Exception)
        {
            ErrorMenu =
                "Ocurrió un error al recuperar " +
                "las opciones del menú.";
        }

        // Si falla la consulta del menú,
        // la sesión permanece iniciada.
        // No se conceden permisos adicionales.
    }

    // ========================================
    // CERRAR SESIÓN
    // ========================================
    public async Task CerrarAsync()
    {
        LimpiarMemoria();

        try
        {
            await _almacen.DeleteAsync(
                ClaveAlmacenamiento);
        }
        catch (Exception)
        {
            // Aunque el navegador no permita
            // borrar la sesión, el acceso actual
            // queda retirado de memoria.
        }

        Inicializada = true;
    }

    // ========================================
    // LIMPIAR INFORMACIÓN DE LA SESIÓN
    // ========================================
    private void LimpiarMemoria()
    {
        _datos = null;

        _http.DefaultRequestHeaders.Authorization = null;

        _opcionesMenu = new List<OpcionMenuDinamica>();

        MenuCargado = false;

        ErrorMenu = "";
    }
}