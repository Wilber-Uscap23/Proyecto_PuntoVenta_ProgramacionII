using Microsoft.AspNetCore.Http;

namespace PuntoVentaAPI.Seguridad;

public sealed class PermisosOperacionesMiddleware
{
    private readonly RequestDelegate _siguiente;

    public PermisosOperacionesMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Estas reglas se aplican únicamente a /api.
        if (!context.Request.Path.StartsWithSegments(
            "/api",
            StringComparison.OrdinalIgnoreCase))
        {
            await _siguiente(context);
            return;
        }

        var ruta = context.Request.Path.Value ?? "";
        var metodo = context.Request.Method;

        // ====================================
        // PERMITIR OPERACIONES DE LECTURA
        // ====================================
        if (HttpMethods.IsGet(metodo) ||
            HttpMethods.IsHead(metodo) ||
            HttpMethods.IsOptions(metodo))
        {
            await _siguiente(context);
            return;
        }

        // ====================================
        // PERMITIR INICIO DE SESIÓN
        // ====================================
        if (ruta.Equals(
            "/api/Auth/login",
            StringComparison.OrdinalIgnoreCase))
        {
            await _siguiente(context);
            return;
        }

        // ====================================
        // PERMITIR CAMBIO DE CONTRASEÑA INICIAL
        // ====================================
        // Esta operación no requiere JWT porque
        // el usuario todavía no ha iniciado sesión.
        //
        // AuthController verificará nuevamente
        // su contraseña temporal antes de guardar
        // la contraseña personal.

        if (HttpMethods.IsPost(metodo) &&
            ruta.Equals(
                "/api/Auth/cambiar-contrasena-inicial",
                StringComparison.OrdinalIgnoreCase))
        {
            await _siguiente(context);
            return;
        }

        // ====================================
        // VERIFICAR AUTENTICACIÓN
        // ====================================
        // Todas las demás operaciones de escritura
        // requieren una sesión válida.

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await Responder(
                context,
                StatusCodes.Status401Unauthorized,
                "Debes iniciar sesión para realizar esta operación.");

            return;
        }

        // ====================================
        // PERMISOS DEL ADMINISTRADOR
        // ====================================
        if (context.User.IsInRole("ADMIN"))
        {
            await _siguiente(context);
            return;
        }

        // ====================================
        // PERMISOS DEL VENDEDOR
        // ====================================
        if (context.User.IsInRole("VENDEDOR"))
        {
            // Registrar ventas nuevas.
            if (HttpMethods.IsPost(metodo) &&
                ruta.Equals(
                    "/api/ventas",
                    StringComparison.OrdinalIgnoreCase))
            {
                await _siguiente(context);
                return;
            }

            // Registrar clientes nuevos.
            if (HttpMethods.IsPost(metodo) &&
                ruta.Equals(
                    "/api/Clientes",
                    StringComparison.OrdinalIgnoreCase))
            {
                await _siguiente(context);
                return;
            }
        }

        // ====================================
        // OPERACIÓN NO AUTORIZADA
        // ====================================
        // CONSULTA no puede realizar escrituras.
        // VENDEDOR solo puede utilizar las
        // operaciones autorizadas anteriormente.

        await Responder(
            context,
            StatusCodes.Status403Forbidden,
            "Tu rol no tiene permisos para modificar esta información.");
    }

    private static Task Responder(
        HttpContext context,
        int codigo,
        string mensaje)
    {
        context.Response.StatusCode = codigo;

        return context.Response.WriteAsJsonAsync(
            new { mensaje });
    }
}