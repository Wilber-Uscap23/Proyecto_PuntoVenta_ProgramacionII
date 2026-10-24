using PuntoVenta.Web.Components;
using PuntoVenta.Web.Services;
using PuntoVenta.Web.Services.Base;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------- Conexión con la API ----------
// ApiSettings:BaseUrl = dirección que usa ESTE servidor para llamar a la API.
// En IIS, si la API está en el mismo servidor: http://localhost:8080/
var urlApi = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("Falta 'ApiSettings:BaseUrl' en appsettings.json.");

builder.Services.AddHttpClient(ApiClienteBase.NombreCliente, client =>
{
    client.BaseAddress = new Uri(urlApi.EndsWith('/') ? urlApi : urlApi + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ---------- Sesión (BASE DE INTEGRACIÓN: Wilber) ----------
builder.Services.AddScoped<SesionService>();

// ---------- Módulo de Rodrigo: menú dinámico y reportes ----------
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddScoped<ReporteJsService>();

// ---------- Módulo de Proveedores y Compras ----------

builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<CompraService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
