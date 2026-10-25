
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.DTOs;
using PuntoVentaAPI.Models;

namespace PuntoVentaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductosController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const long MaxImagen = 2 * 1024 * 1024;

    public ProductosController(
        PuntoVentaDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // CONSULTAR TODOS LOS PRODUCTOS
    [HttpGet]
    public async Task<IActionResult> ObtenerProductos()
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .OrderBy(p => p.IdProducto)
            .Select(p => new
            {
                p.IdProducto,
                p.Codigo,
                p.Descripcion,
                p.PrecioCosto,
                p.PrecioVenta,
                p.Existencia,
                p.IdMarca,
                p.FechaIngreso,
                p.ImagenUrl,
                p.Estado
            })
            .ToListAsync();

        return Ok(productos);
    }

    // CONSULTAR PRODUCTO POR ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerProductoPorId(int id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .Where(p => p.IdProducto == id)
            .Select(p => new
            {
                p.IdProducto,
                p.Codigo,
                p.Descripcion,
                p.PrecioCosto,
                p.PrecioVenta,
                p.Existencia,
                p.IdMarca,
                p.FechaIngreso,
                p.ImagenUrl,
                p.Estado
            })
            .FirstOrDefaultAsync();

        if (producto == null)
            return NotFound(new { mensaje = "Producto no encontrado." });

        return Ok(producto);
    }

    // REGISTRAR PRODUCTO
    [HttpPost]
    public async Task<IActionResult> RegistrarProducto(
        [FromBody] CrearProductoDto datos)
    {
        var error = ValidarDatos(
            datos.Codigo,
            datos.Descripcion,
            datos.PrecioCosto,
            datos.PrecioVenta,
            datos.Existencia,
            datos.ImagenUrl);

        if (error != null)
            return BadRequest(new { mensaje = error });

        var codigo = datos.Codigo.Trim();

        if (await _context.Productos.AnyAsync(
            p => p.Codigo == codigo))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un producto con ese código."
            });
        }

        if (!await _context.Marcas.AnyAsync(
            m => m.IdMarca == datos.IdMarca && m.Estado == true))
        {
            return BadRequest(new
            {
                mensaje = "Selecciona una marca activa."
            });
        }

        var producto = new Producto
        {
            Codigo = codigo,
            Descripcion = datos.Descripcion!.Trim(),
            PrecioCosto = datos.PrecioCosto,
            PrecioVenta = datos.PrecioVenta,
            Existencia = datos.Existencia,
            IdMarca = datos.IdMarca,
            ImagenUrl = LimpiarUrl(datos.ImagenUrl),
            Estado = true
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenerProductoPorId),
            new { id = producto.IdProducto },
            new
            {
                mensaje = "Producto registrado correctamente.",
                producto.IdProducto
            });
    }

    // EDITAR PRODUCTO
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarProducto(
        int id,
        [FromBody] ActualizarProductoDto datos)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.IdProducto == id);

        if (producto == null)
            return NotFound(new { mensaje = "Producto no encontrado." });

        var error = ValidarDatos(
            datos.Codigo,
            datos.Descripcion,
            datos.PrecioCosto,
            datos.PrecioVenta,
            datos.Existencia,
            datos.ImagenUrl);

        if (error != null)
            return BadRequest(new { mensaje = error });

        var codigo = datos.Codigo.Trim();

        if (await _context.Productos.AnyAsync(
            p => p.Codigo == codigo && p.IdProducto != id))
        {
            return Conflict(new
            {
                mensaje = "El código pertenece a otro producto."
            });
        }

        // Permite conservar una marca inactiva que ya tenía
        // el producto, pero no asignarle otra marca inactiva.
        bool marcaValida = await _context.Marcas.AnyAsync(m =>
            m.IdMarca == datos.IdMarca &&
            (m.Estado == true || m.IdMarca == producto.IdMarca));

        if (!marcaValida)
        {
            return BadRequest(new
            {
                mensaje = "Selecciona una marca válida."
            });
        }

        producto.Codigo = codigo;
        producto.Descripcion = datos.Descripcion!.Trim();
        producto.PrecioCosto = datos.PrecioCosto;
        producto.PrecioVenta = datos.PrecioVenta;
        producto.Existencia = datos.Existencia;
        producto.IdMarca = datos.IdMarca;
        producto.ImagenUrl = LimpiarUrl(datos.ImagenUrl);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto actualizado correctamente.",
            producto.IdProducto
        });
    }

    // DESACTIVAR PRODUCTO
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DesactivarProducto(int id)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.IdProducto == id);

        if (producto == null)
            return NotFound(new { mensaje = "Producto no encontrado." });

        if (producto.Estado != true)
        {
            return BadRequest(new
            {
                mensaje = "El producto ya está inactivo."
            });
        }

        producto.Estado = false;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto desactivado correctamente.",
            estado = producto.Estado
        });
    }

    // REACTIVAR PRODUCTO
    [HttpPut("{id:int}/activar")]
    public async Task<IActionResult> ActivarProducto(int id)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.IdProducto == id);

        if (producto == null)
            return NotFound(new { mensaje = "Producto no encontrado." });

        if (producto.Estado == true)
        {
            return BadRequest(new
            {
                mensaje = "El producto ya está activo."
            });
        }

        bool marcaActiva = await _context.Marcas.AnyAsync(
            m => m.IdMarca == producto.IdMarca && m.Estado == true);

        if (!marcaActiva)
        {
            return BadRequest(new
            {
                mensaje = "No se puede activar el producto porque su marca está inactiva."
            });
        }

        producto.Estado = true;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Producto activado correctamente.",
            estado = producto.Estado
        });
    }

    // SUBIR IMAGEN AL SERVIDOR
    [HttpPost("imagen")]
    [RequestSizeLimit(4_000_000)]
    public async Task<IActionResult> SubirImagen(
        [FromForm] IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new
            {
                mensaje = "Selecciona una imagen."
            });
        }

        if (archivo.Length > MaxImagen)
        {
            return BadRequest(new
            {
                mensaje = "La imagen no debe superar 2 MB."
            });
        }

        await using var entrada = archivo.OpenReadStream();

        // Validar la firma real, no solamente la extensión.
        var cabecera = new byte[12];
        int leidos = await entrada.ReadAsync(cabecera.AsMemory());

        bool jpg = leidos >= 3 &&
            cabecera[0] == 0xFF &&
            cabecera[1] == 0xD8 &&
            cabecera[2] == 0xFF;

        bool png = leidos >= 8 &&
            cabecera[0] == 0x89 &&
            cabecera[1] == 0x50 &&
            cabecera[2] == 0x4E &&
            cabecera[3] == 0x47 &&
            cabecera[4] == 0x0D &&
            cabecera[5] == 0x0A &&
            cabecera[6] == 0x1A &&
            cabecera[7] == 0x0A;

        bool webp = leidos >= 12 &&
            System.Text.Encoding.ASCII.GetString(cabecera, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII.GetString(cabecera, 8, 4) == "WEBP";

        if (!jpg && !png && !webp)
        {
            return BadRequest(new
            {
                mensaje = "Solo se permiten imágenes JPG, PNG o WebP."
            });
        }

        string extension = jpg ? ".jpg" : png ? ".png" : ".webp";
        string nombre = Guid.NewGuid().ToString("N") + extension;

        string carpeta = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot",
            "images",
            "productos");

        Directory.CreateDirectory(carpeta);

        string ruta = Path.Combine(carpeta, nombre);

        await using (var salida = new FileStream(
            ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await salida.WriteAsync(cabecera.AsMemory(0, leidos));
            await entrada.CopyToAsync(salida);
        }

        return Ok(new
        {
            imagenUrl = "/images/productos/" + nombre
        });
    }

    // VALIDACIONES DE LA API
    private static string? ValidarDatos(
        string? codigo,
        string? descripcion,
        decimal precioCosto,
        decimal precioVenta,
        int existencia,
        string? imagenUrl)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return "El código es obligatorio.";

        if (codigo.Trim().Length > 50)
            return "El código no debe superar 50 caracteres.";

        if (string.IsNullOrWhiteSpace(descripcion))
            return "La descripción es obligatoria.";

        if (descripcion.Trim().Length > 255)
            return "La descripción no debe superar 255 caracteres.";

        if (precioCosto < 0 || precioCosto > 9999999999.99m)
            return "El precio de costo es inválido.";

        if (precioVenta <= 0 || precioVenta > 9999999999.99m)
            return "El precio de venta debe ser mayor que cero.";

        if (decimal.Round(precioCosto, 2) != precioCosto ||
            decimal.Round(precioVenta, 2) != precioVenta)
        {
            return "Los precios solo pueden tener dos decimales.";
        }

        if (existencia < 0)
            return "La existencia no puede ser negativa.";

        if (!UrlImagenValida(imagenUrl))
            return "La URL de imagen debe ser HTTP, HTTPS o una ruta local válida.";

        return null;
    }

    private static string? LimpiarUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    private static bool UrlImagenValida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        url = url.Trim();

        if (url.Length > 500)
            return false;

        if (url.StartsWith("/") && !url.StartsWith("//"))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps);
    }
}
