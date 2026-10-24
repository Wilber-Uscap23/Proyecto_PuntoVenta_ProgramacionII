// Módulo: Reportes — Rodrigo Rodríguez
using Microsoft.JSInterop;

namespace PuntoVenta.Web.Services;

/// <summary>Imprimir el reporte y descargar el CSV (usa wwwroot/js/reportes.js).</summary>
public sealed class ReporteJsService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? modulo;

    public async Task DescargarCsvAsync(string nombreArchivo, string contenido)
        => await (await ModuloAsync()).InvokeVoidAsync("descargarArchivo", nombreArchivo, contenido, "text/csv;charset=utf-8");

    public async Task ImprimirAsync()
        => await (await ModuloAsync()).InvokeVoidAsync("imprimir");

    private async Task<IJSObjectReference> ModuloAsync()
        => modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/reportes.js");

    public async ValueTask DisposeAsync()
    {
        if (modulo is not null)
        {
            try
            {
                await modulo.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // el usuario cerró la pestaña: no hay nada que liberar
            }
        }
    }
}
