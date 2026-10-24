// Módulo: Menú dinámico y Reportes — Rodrigo Rodríguez
using System.Net;

namespace PuntoVenta.Web.Services;

/// <summary>Error al llamar a la API, con un mensaje listo para mostrar al usuario.</summary>
public class ApiException(string mensaje, HttpStatusCode? codigo = null) : Exception(mensaje)
{
    public HttpStatusCode? Codigo { get; } = codigo;

    /// <summary>true cuando la API respondió 401 (no hay sesión o el token venció).</summary>
    public bool SinAutorizacion => Codigo == HttpStatusCode.Unauthorized;
}
