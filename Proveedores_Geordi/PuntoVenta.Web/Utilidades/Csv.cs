// Módulo: Reportes — Rodrigo Rodríguez
using System.Globalization;
using System.Text;

namespace PuntoVenta.Web.Utilidades;

/// <summary>
/// Arma archivos CSV que Excel abre bien (UTF-8 con BOM para las tildes y la ñ).
/// </summary>
public class Csv
{
    private readonly StringBuilder _sb = new();

    public Csv Fila(params object?[] valores)
    {
        _sb.AppendJoin(',', valores.Select(Celda));
        _sb.Append("\r\n");
        return this;
    }

    /// <summary>Contenido final (incluye BOM).</summary>
    public override string ToString() => "﻿" + _sb;

    private static string Celda(object? valor)
    {
        var texto = valor switch
        {
            null => string.Empty,
            DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            decimal m => m.ToString("0.00", CultureInfo.InvariantCulture),
            bool b => b ? "Sí" : "No",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => valor.ToString() ?? string.Empty
        };

        // Evita que Excel interprete texto como fórmula (=, +, -, @).
        if (texto.Length > 0 && "=+-@".Contains(texto[0]) && valor is string)
        {
            texto = "'" + texto;
        }

        return texto.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + texto.Replace("\"", "\"\"") + "\""
            : texto;
    }
}
