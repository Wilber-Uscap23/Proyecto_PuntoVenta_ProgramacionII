
using PuntoVentaWeb.Components;
using PuntoVentaWeb.Servicios;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONFIGURACIÓN BLAZOR
// ==========================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ==========================================
// CONEXIÓN CON NUESTRA API
// ==========================================
builder.Services.AddHttpClient(
    "PuntoVentaAPI",
    client =>
    {
        client.BaseAddress =
            new Uri("http://localhost:5235/");
    });

// Un HttpClient por sesión interactiva.
// Conservamos la inyección que ya utilizan
// Productos, Compras, Ventas y demás páginas.
builder.Services.AddScoped(sp =>
{
    var factory =
        sp.GetRequiredService<IHttpClientFactory>();

    return factory.CreateClient("PuntoVentaAPI");
});

// ==========================================
// SESIÓN PROTEGIDA DEL USUARIO
// ==========================================
builder.Services.AddScoped<ProtectedSessionStorage>();

builder.Services.AddScoped<SesionUsuario>();

// ==========================================
// CREAR APLICACIÓN
// ==========================================
var app = builder.Build();

// ==========================================
// CONFIGURACIÓN HTTP
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
