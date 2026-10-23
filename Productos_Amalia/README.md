# Módulo de Marcas y Productos (Amalia Rosales)

CRUD de **Marcas** y **Productos** con imagen, API REST con JWT y pantallas Blazor.
Las tablas son `marcas` y `productos` de `database/PuntoVentaDB.sql` (no se crean tablas nuevas).

```
Productos_Amalia/
├── ApiProductos/   API REST (.NET 10, EF Core, MySQL, JWT)   -> puerto 5301
├── WebProductos/   Blazor (Bootstrap 5)                      -> puerto 5302
└── Database/       Datos_Marcas_Productos.sql (datos de ejemplo)
```

## Cómo probarlo solo (sin el login de Wilber)
1. Importar `database/PuntoVentaDB.sql` en MySQL y luego `Database/Datos_Marcas_Productos.sql`.
2. En `ApiProductos/appsettings.json` poner la contraseña real de MySQL en `ConnectionStrings:PuntoVentaDB`
   (no subir la contraseña real a GitHub).
3. Ejecutar `ApiProductos` y después `WebProductos` (Visual Studio: varios proyectos de inicio).
4. Abrir http://localhost:5302
   En **Development** la API tiene `Seguridad:ExigirJwt = false`, por eso funciona sin iniciar sesión.

## Endpoints (todos `[Authorize]`: exigen JWT en producción)
| Método | Ruta | Descripción |
|---|---|---|
| GET | `api/marcas?buscar=` | Listar / buscar marcas |
| GET/PUT/DELETE | `api/marcas/{id}` | Consultar / modificar / eliminar |
| POST | `api/marcas` | Crear |
| GET | `api/productos?buscar=&idMarca=` | Listar / buscar / filtrar por marca |
| GET/PUT/DELETE | `api/productos/{id}` | Consultar / modificar / eliminar |
| POST | `api/productos` | Crear |
| POST | `api/productos/{id}/imagen` | Subir imagen (multipart, campo `archivo`) |

Respuestas de error: `{ "mensaje": "..." }` (409 duplicado / tiene registros asociados, 404, 400 validación).

## Reglas implementadas
- **Precio de venta = costo + 25 %**, calculado SIEMPRE en la API (lo que envíe el cliente se ignora).
- **Código** de producto y **nombre** de marca únicos (mensaje claro, no error 500).
- No se puede eliminar una marca con productos, ni un producto con compras/ventas (mensaje claro).
- **Imagen:** se guarda el archivo en una carpeta del servidor y en MySQL solo la ruta
  (`/images/productos/xxxx.jpg`). Se valida extensión, tamaño (5 MB) y contenido real del archivo.
  Al cambiar o eliminar el producto se borra la imagen anterior.
- Validaciones en Blazor (DataAnnotations) y en la API: obligatorios, longitudes, precio > 0,
  existencia ≥ 0, fecha válida y no futura, marca existente.

## Para publicar en IIS (coordinar con Rodrigo)
- `WebProductos/appsettings.json`:
  - `Api:BaseUrl` = dirección con la que el servidor llama a la API (puede ser `http://localhost:8080/`).
  - `Api:UrlPublica` = **IP del servidor** (ej. `http://10.0.0.4:8080`). Con ella el navegador de otra PC
    carga las imágenes. No usar localhost.
- `ApiProductos/appsettings.json`: `Imagenes:Carpeta` = carpeta fuera del sitio publicado
  (ej. `C:\PuntoVenta\imagenes\productos`) para que no se borren al volver a publicar.
  La cuenta del Application Pool de IIS necesita permiso de **escritura** en esa carpeta.
- En producción dejar `Seguridad:ExigirJwt = true` y usar la **misma sección `Jwt`** (Key, Issuer, Audience) que el login de Wilber.

## Integración con el login de Wilber (cuando esté listo)
`WebProductos` no depende de cómo se guarde la sesión: solo usa `ITokenProvider`. Para integrarlo, crear:

```csharp
public class TokenProviderSesion(SesionService sesion) : ITokenProvider
{
    public Task<string?> ObtenerTokenAsync() => sesion.ObtenerTokenAsync();
}
// Program.cs: reemplazar AddSingleton<ITokenProvider, TokenProviderConfiguracion>() por
builder.Services.AddScoped<ITokenProvider, TokenProviderSesion>();
```

## Pendientes de coordinación
- **Geordi:** su stub `Producto` usa la columna `imagen`; en la base de datos real es `imagen_url`.
  Además, Compras debe actualizar `precio_costo`, `precio_venta` (+25 %) y `existencia` en `productos`.
- **Wilber:** confirmar el proveedor MySQL común (este módulo usa `Microting.EntityFrameworkCore.MySql`, igual que ApiVentas).
- **Rodrigo:** las rutas `/marcas` y `/productos` de este módulo ya coinciden con la tabla `menu`.
