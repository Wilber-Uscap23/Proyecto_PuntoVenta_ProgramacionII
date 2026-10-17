using WebVentas.Components;
using WebVentas.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Dirección de la API (ApiVentas). Debe coincidir con el puerto https que usa tu API.
var apiUrl = new Uri("https://localhost:7120/");

builder.Services.AddHttpClient<VentasService>(c => c.BaseAddress = apiUrl);
builder.Services.AddHttpClient<ClientesService>(c => c.BaseAddress = apiUrl);
builder.Services.AddHttpClient<BusquedaService>(c => c.BaseAddress = apiUrl);   // NUEVO: búsqueda de cliente por NIT y empleado

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();