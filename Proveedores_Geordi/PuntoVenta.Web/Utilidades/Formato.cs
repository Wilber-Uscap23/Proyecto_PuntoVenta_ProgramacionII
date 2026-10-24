// Módulo: Reportes — Rodrigo Rodríguez
using System.Globalization;

namespace PuntoVenta.Web.Utilidades;

/// <summary>Formatos de Guatemala para mostrar dinero, números y fechas en los reportes.</summary>
public static class Formato
{
    private static readonly CultureInfo Cultura = ObtenerCultura();

    public static string Moneda(decimal valor) => "Q " + valor.ToString("N2", Cultura);
    public static string Numero(int valor) => valor.ToString("N0", Cultura);
    public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy", Cultura);
    public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    private static CultureInfo ObtenerCultura()
    {
        try
        {
            return CultureInfo.GetCultureInfo("es-GT");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture; // servidor sin datos de cultura
        }
    }
}
