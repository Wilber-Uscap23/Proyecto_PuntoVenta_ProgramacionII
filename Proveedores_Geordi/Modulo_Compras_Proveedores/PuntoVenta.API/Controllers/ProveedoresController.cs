// Módulo: Proveedores y Compras
// CRUD de proveedores con búsqueda y validaciones.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuntoVenta.API.Data;
using PuntoVenta.API.Models;
using PuntoVenta.Shared.Dtos.Proveedores;
using PuntoVenta.Shared.Dtos.Reportes;

namespace PuntoVenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProveedoresController(AppDbContext db) : ControllerBase
{
    private const int LargoMaximoBusqueda = 100;

    // ------------------------------------------------------------------
    //  GET api/proveedores?buscar=&estado=todos|activos|inactivos
    // ------------------------------------------------------------------
    /// <summary>Lista proveedores. La búsqueda aplica a nombre, NIT, teléfono, correo y contacto.</summary>
    [HttpGet]
    [ProducesResponseType<List<ProveedorDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ProveedorDto>>> Listar(
        [FromQuery] string? buscar,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        buscar = buscar?.Trim();
        if (buscar?.Length > LargoMaximoBusqueda)
        {
            return Error($"El texto a buscar no puede tener más de {LargoMaximoBusqueda} caracteres.");
        }

        var estadoNormalizado = (estado ?? "todos").Trim().ToLowerInvariant();
        if (estadoNormalizado is not ("todos" or "activos" or "inactivos"))
        {
            return Error("El estado debe ser: todos, activos o inactivos.");
        }

        var consulta = db.Proveedores.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
        {
            consulta = consulta.Where(p =>
                p.Nombre.Contains(buscar) ||
                (p.Nit != null && p.Nit.Contains(buscar)) ||
                (p.Telefono != null && p.Telefono.Contains(buscar)) ||
                (p.Correo != null && p.Correo.Contains(buscar)) ||
                (p.Contacto != null && p.Contacto.Contains(buscar)));
        }
        if (estadoNormalizado == "activos")
        {
            consulta = consulta.Where(p => p.Estado);
        }
        else if (estadoNormalizado == "inactivos")
        {
            consulta = consulta.Where(p => !p.Estado);
        }

        var proveedores = await consulta
            .OrderBy(p => p.Nombre)
            .Select(p => new ProveedorDto
            {
                IdProveedor = p.IdProveedor,
                Nombre = p.Nombre,
                Nit = p.Nit,
                Telefono = p.Telefono,
                Correo = p.Correo,
                Direccion = p.Direccion,
                Contacto = p.Contacto,
                FechaRegistro = p.FechaRegistro,
                Estado = p.Estado
            })
            .ToListAsync(ct);

        return Ok(proveedores);
    }

    // ------------------------------------------------------------------
    //  GET api/proveedores/5
    // ------------------------------------------------------------------
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProveedorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProveedorDto>> Obtener(int id, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProveedor == id, ct);

        return proveedor is null ? NoEncontrado() : Ok(Mapear(proveedor));
    }

    // ------------------------------------------------------------------
    //  POST api/proveedores
    // ------------------------------------------------------------------
    [HttpPost]
    [ProducesResponseType<ProveedorDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDto>> Crear([FromBody] ProveedorGuardarDto dto, CancellationToken ct)
    {
        var proveedor = new Proveedor { FechaRegistro = DateTime.Now };
        Aplicar(proveedor, dto);

        if (await NitDuplicadoAsync(proveedor.Nit, null, ct))
        {
            return Duplicado();
        }

        db.Proveedores.Add(proveedor);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dos usuarios guardando el mismo NIT al mismo tiempo: lo frena el índice UNIQUE.
            return Duplicado();
        }

        return CreatedAtAction(nameof(Obtener), new { id = proveedor.IdProveedor }, Mapear(proveedor));
    }

    // ------------------------------------------------------------------
    //  PUT api/proveedores/5
    // ------------------------------------------------------------------
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProveedorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDto>> Actualizar(int id, [FromBody] ProveedorGuardarDto dto, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.IdProveedor == id, ct);
        if (proveedor is null)
        {
            return NoEncontrado();
        }

        Aplicar(proveedor, dto);

        if (await NitDuplicadoAsync(proveedor.Nit, id, ct))
        {
            return Duplicado();
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Duplicado();
        }

        return Ok(Mapear(proveedor));
    }

    // ------------------------------------------------------------------
    //  DELETE api/proveedores/5
    // ------------------------------------------------------------------
    /// <summary>
    /// Elimina el proveedor solo si nunca se le hizo una compra. Si ya tiene compras
    /// se rechaza (la llave foránea es RESTRICT) y se sugiere desactivarlo.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorApiDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.IdProveedor == id, ct);
        if (proveedor is null)
        {
            return NoEncontrado();
        }

        if (await db.Compras.AnyAsync(c => c.IdProveedor == id, ct))
        {
            return Conflict(new ErrorApiDto
            {
                Mensaje = "Este proveedor ya tiene compras registradas y no se puede eliminar. Edítelo y desmarque «Activo» para dejar de usarlo."
            });
        }

        db.Proveedores.Remove(proveedor);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ------------------------------------------------------------------
    //  Auxiliares
    // ------------------------------------------------------------------
    private async Task<bool> NitDuplicadoAsync(string? nit, int? idActual, CancellationToken ct)
    {
        if (nit is null)
        {
            return false;
        }
        return await db.Proveedores.AnyAsync(p => p.Nit == nit && p.IdProveedor != idActual, ct);
    }

    /// <summary>Limpia espacios y convierte vacío en null (la columna NIT es UNIQUE: no pueden haber dos "").</summary>
    private static void Aplicar(Proveedor proveedor, ProveedorGuardarDto dto)
    {
        proveedor.Nombre = dto.Nombre.Trim();
        proveedor.Nit = Limpiar(dto.Nit);
        proveedor.Telefono = Limpiar(dto.Telefono);
        proveedor.Correo = Limpiar(dto.Correo);
        proveedor.Direccion = Limpiar(dto.Direccion);
        proveedor.Contacto = Limpiar(dto.Contacto);
        proveedor.Estado = dto.Estado;
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static ProveedorDto Mapear(Proveedor p) => new()
    {
        IdProveedor = p.IdProveedor,
        Nombre = p.Nombre,
        Nit = p.Nit,
        Telefono = p.Telefono,
        Correo = p.Correo,
        Direccion = p.Direccion,
        Contacto = p.Contacto,
        FechaRegistro = p.FechaRegistro,
        Estado = p.Estado
    };

    private BadRequestObjectResult Error(string mensaje) => BadRequest(new ErrorApiDto { Mensaje = mensaje });

    private NotFoundObjectResult NoEncontrado() => NotFound(new ErrorApiDto { Mensaje = "No se encontró el proveedor solicitado." });

    private ConflictObjectResult Duplicado() => Conflict(new ErrorApiDto { Mensaje = "Ya existe otro proveedor con ese NIT." });
}
