using ApiProductos.Data;
using ApiProductos.Dtos;
using ApiProductos.Models;
using ApiProductos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiProductos.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductosController(AppDbContext db, AlmacenImagenes imagenes) : ControllerBase
{
    /// <summary>Regla del proyecto: precio de venta = precio de costo + 25 %.</summary>
    private const decimal PorcentajeGanancia = 0.25m;

    private static decimal CalcularPrecioVenta(decimal costo) =>
        Math.Round(costo * (1 + PorcentajeGanancia), 2, MidpointRounding.AwayFromZero);

    // GET api/productos?buscar=monitor&idMarca=2
    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> Listar([FromQuery] string? buscar, [FromQuery] int? idMarca)
    {
        var consulta = db.Productos.AsNoTracking();

        if (idMarca is > 0)
            consulta = consulta.Where(p => p.IdMarca == idMarca);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(p => p.Codigo.Contains(texto)
                                        || p.Descripcion.Contains(texto)
                                        || (p.Marca != null && p.Marca.Nombre.Contains(texto)));
        }

        return await Proyectar(consulta).OrderBy(p => p.Descripcion).ToListAsync();
    }

    // GET api/productos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductoDto>> Obtener(int id)
    {
        var dto = await BuscarDtoAsync(id);
        return dto is null ? NotFound(new { mensaje = "El producto no existe." }) : Ok(dto);
    }

    // POST api/productos
    [HttpPost]
    public async Task<ActionResult<ProductoDto>> Crear(ProductoDto dto)
    {
        var error = await ValidarReglasAsync(dto, idActual: 0);
        if (error is not null) return error;

        var producto = new Producto();
        Aplicar(producto, dto);
        db.Productos.Add(producto);

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Conflict(new { mensaje = $"Ya existe un producto con el código '{dto.Codigo.Trim()}'." });
        }

        return CreatedAtAction(nameof(Obtener), new { id = producto.IdProducto }, await BuscarDtoAsync(producto.IdProducto));
    }

    // PUT api/productos/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductoDto>> Modificar(int id, ProductoDto dto)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound(new { mensaje = "El producto no existe." });

        var error = await ValidarReglasAsync(dto, idActual: id);
        if (error is not null) return error;

        Aplicar(producto, dto); // la imagen NO se toca aquí (ver SubirImagen)

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Conflict(new { mensaje = $"Ya existe otro producto con el código '{dto.Codigo.Trim()}'." });
        }

        return Ok(await BuscarDtoAsync(id));
    }

    // DELETE api/productos/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound(new { mensaje = "El producto no existe." });

        var rutaImagen = producto.ImagenUrl;
        db.Productos.Remove(producto);

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // compras_detalle / ventas_detalle tienen ON DELETE RESTRICT
            return Conflict(new { mensaje = "No se puede eliminar el producto porque tiene compras o ventas registradas." });
        }

        imagenes.Eliminar(rutaImagen);
        return NoContent();
    }

    // POST api/productos/5/imagen   (multipart/form-data, campo "archivo")
    [HttpPost("{id:int}/imagen")]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<ProductoDto>> SubirImagen(int id, IFormFile archivo)
    {
        var producto = await db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound(new { mensaje = "El producto no existe." });

        var (url, error) = await imagenes.GuardarAsync(archivo);
        if (url is null)
            return BadRequest(new { mensaje = error });

        var rutaAnterior = producto.ImagenUrl;
        producto.ImagenUrl = url;          // en MySQL solo se guarda la ruta
        await db.SaveChangesAsync();
        imagenes.Eliminar(rutaAnterior);   // se borra la imagen vieja para no acumular archivos

        return Ok(await BuscarDtoAsync(id));
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<ActionResult?> ValidarReglasAsync(ProductoDto dto, int idActual)
    {
        if (!await db.Marcas.AnyAsync(m => m.IdMarca == dto.IdMarca))
            return BadRequest(new { mensaje = "La marca seleccionada no existe." });

        var codigo = dto.Codigo.Trim();
        if (await db.Productos.AnyAsync(p => p.Codigo == codigo && p.IdProducto != idActual))
            return Conflict(new { mensaje = $"Ya existe un producto con el código '{codigo}'." });

        return null;
    }

    private static void Aplicar(Producto p, ProductoDto dto)
    {
        p.Codigo = dto.Codigo.Trim();
        p.Descripcion = dto.Descripcion.Trim();
        p.IdMarca = dto.IdMarca;
        p.PrecioCosto = Math.Round(dto.PrecioCosto, 2, MidpointRounding.AwayFromZero);
        p.PrecioVenta = CalcularPrecioVenta(p.PrecioCosto); // siempre calculado en el servidor
        p.Existencia = dto.Existencia;
        p.FechaIngreso = dto.FechaIngreso;
        p.Estado = dto.Estado;
    }

    private static IQueryable<ProductoDto> Proyectar(IQueryable<Producto> consulta) =>
        consulta.Select(p => new ProductoDto
        {
            IdProducto = p.IdProducto,
            Codigo = p.Codigo,
            Descripcion = p.Descripcion,
            IdMarca = p.IdMarca,
            NombreMarca = p.Marca != null ? p.Marca.Nombre : null,
            PrecioCosto = p.PrecioCosto,
            PrecioVenta = p.PrecioVenta,
            Existencia = p.Existencia,
            FechaIngreso = p.FechaIngreso,
            ImagenUrl = p.ImagenUrl,
            Estado = p.Estado
        });

    private Task<ProductoDto?> BuscarDtoAsync(int id) =>
        Proyectar(db.Productos.AsNoTracking().Where(p => p.IdProducto == id)).FirstOrDefaultAsync();
}
