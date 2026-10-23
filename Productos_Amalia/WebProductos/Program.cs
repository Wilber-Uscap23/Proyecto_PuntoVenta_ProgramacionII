using WebProductos.Components;
using WebProductos.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------- Conexión con la API ----------
// Api:BaseUrl    = dirección que usa ESTE servidor para llamar a la API (puede ser localhost).
// Api:UrlPublica = dirección con la que el NAVEGADOR del usuario carga las imágenes.
//                  Debe ser la IP/nombre del servidor (nunca localhost si se entra desde otra PC).
var urlApi = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Falta 'Api:BaseUrl' en appsettings.json.");

builder.Services.AddHttpClient(ApiCliente.NombreCliente, cliente =>
{
    cliente.BaseAddress = new Uri(urlApi.EndsWith('/') ? urlApi : urlApi + "/");
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

// ---------- Token JWT ----------
// INTEGRACIÓN: cuando Wilber entregue el login, se reemplaza esta línea por un ITokenProvider
// que lea el token de su SesionService (ver README del módulo).
builder.Services.AddSingleton<ITokenProvider, TokenProviderConfiguracion>();

builder.Services.AddScoped<ApiCliente>();
builder.Services.AddScoped<MarcasService>();
builder.Services.AddScoped<ProductosService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
