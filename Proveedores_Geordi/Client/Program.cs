using ComprasApp.Client;
using ComprasApp.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL de la API tomada de wwwroot/appsettings.json (IP/nombre del servidor, NO localhost).
var apiUrl = builder.Configuration["ApiBaseUrl"]
             ?? throw new InvalidOperationException("Falta ApiBaseUrl en appsettings.json");

builder.Services.AddSingleton<TokenStore>();
builder.Services.AddTransient<AuthHandler>();
builder.Services.AddHttpClient("Api", c => c.BaseAddress = new Uri(apiUrl)).AddHttpMessageHandler<AuthHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

await builder.Build().RunAsync();
