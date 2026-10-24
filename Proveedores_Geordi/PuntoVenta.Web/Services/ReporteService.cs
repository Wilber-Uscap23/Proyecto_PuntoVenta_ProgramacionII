// Módulo: Reportes — Rodrigo Rodríguez
using System.Globalization;
using PuntoVenta.Shared.Dtos.Reportes;
using PuntoVenta.Web.Services.Base;

namespace PuntoVenta.Web.Services;

/// <summary>Llama a los endpoints api/reportes/* de la API.</summary>
public class ReporteService(IHttpClientFactory httpClientFactory, SesionService sesion, IConfiguration config)
    : ApiClienteBase(httpClientFactory, sesion)
{
    public Task<FiltrosReporteDto> ObtenerFiltrosAsync(CancellationToken ct = default)
        => GetAsync<FiltrosReporteDto>("api/reportes/filtros", ct);

    public Task<ReporteProductosDto> ProductosAsync(string? buscar, int? idMarca, string estado, CancellationToken ct = default)
        => GetAsync<ReporteProductosDto>(ArmarUrl("api/reportes/productos",
            ("buscar", buscar), ("idMarca", idMarca), ("estado", estado)), ct);

    public Task<ReporteExistenciasDto> ExistenciasAsync(int? idMarca, int stockMinimo, bool soloBajas, CancellationToken ct = default)
        => GetAsync<ReporteExistenciasDto>(ArmarUrl("api/reportes/existencias",
            ("idMarca", idMarca), ("stockMinimo", stockMinimo), ("soloBajas", soloBajas)), ct);

    public Task<ReporteComprasDto> ComprasAsync(DateTime? desde, DateTime? hasta, int? idProveedor, string estado, CancellationToken ct = default)
        => GetAsync<ReporteComprasDto>(ArmarUrl("api/reportes/compras",
            ("desde", desde), ("hasta", hasta), ("idProveedor", idProveedor), ("estado", estado)), ct);

    public Task<ReporteVentasDto> VentasAsync(DateTime? desde, DateTime? hasta, int? idCliente, int? idEmpleado, string estado, CancellationToken ct = default)
        => GetAsync<ReporteVentasDto>(ArmarUrl("api/reportes/ventas",
            ("desde", desde), ("hasta", hasta), ("idCliente", idCliente), ("idEmpleado", idEmpleado), ("estado", estado)), ct);

    /// <summary>
    /// Convierte la ruta de imagen guardada en MySQL en una URL que el NAVEGADOR pueda abrir.
    /// Si es relativa (ej. "/imagenes/productos/p1.jpg") se le antepone ApiSettings:UrlPublica.
    /// </summary>
    public string? ResolverUrlImagen(string? imagenUrl)
    {
        if (string.IsNullOrWhiteSpace(imagenUrl))
        {
            return null;
        }
        if (Uri.TryCreate(imagenUrl, UriKind.Absolute, out var absoluta) &&
            (absoluta.Scheme == Uri.UriSchemeHttp || absoluta.Scheme == Uri.UriSchemeHttps))
        {
            return imagenUrl;
        }

        var baseUrl = config["ApiSettings:UrlPublica"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = config["ApiSettings:BaseUrl"];
        }
        return $"{baseUrl?.TrimEnd('/')}/{imagenUrl.Replace('\\', '/').TrimStart('/')}";
    }

    private static string ArmarUrl(string ruta, params (string Nombre, object? Valor)[] parametros)
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
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => valor.ToString()
            };
            if (texto is not null)
            {
                partes.Add($"{nombre}={Uri.EscapeDataString(texto)}");
            }
        }
        return partes.Count == 0 ? ruta : $"{ruta}?{string.Join('&', partes)}";
    }
}
