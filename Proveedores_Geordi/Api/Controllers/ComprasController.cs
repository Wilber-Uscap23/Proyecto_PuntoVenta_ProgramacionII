using ComprasApp.Api.Data;
using ComprasApp.Api.Dtos;
using ComprasApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComprasApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ComprasController(AppDbContext db) : ControllerBase
{
    private const decimal Margen = 0.25m; // 25 % sobre el precio de costo

    [HttpGet]
    public async Task<ActionResult<List<CompraResumenDto>>> Listar() =>
        await db.Compras.AsNoTracking()
            .OrderByDescending(c => c.Fecha)
            .Select(c => new CompraResumenDto(c.IdCompra, c.Fecha, c.IdProveedor, c.Proveedor!.Nombre, c.Total))
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompraResponseDto>> Obtener(int id)
    {
        var dto = await BuscarAsync(id);
        return dto is null ? NotFound(new { message = "Compra no encontrada." }) : dto;
    }

    [HttpPost]
    public async Task<ActionResult<CompraResponseDto>> Crear(CompraCreateDto dto)
    {
        if (!await db.Proveedores.AnyAsync(p => p.IdProveedor == dto.IdProveedor))
            return BadRequest(new { message = "El proveedor seleccionado no existe." });

        var ids = dto.Detalles.Select(d => d.IdProducto).Distinct().ToList();
        var encontrados = await db.Productos.CountAsync(p => ids.Contains(p.IdProducto));
        if (encontrados != ids.Count)
            return BadRequest(new { message = "Uno o más productos no existen." });

        // Todo o nada: compra + detalle + actualización de productos en una sola transacción.
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var compra = new Compra { Fecha = DateTime.Now, IdProveedor = dto.IdProveedor };
            foreach (var d in dto.Detalles)
            {
                compra.Detalles.Add(new CompraDetalle
                {
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioCosto = d.PrecioCosto,
                    Subtotal = Math.Round(d.Cantidad * d.PrecioCosto, 2)
                });
            }
            compra.Total = compra.Detalles.Sum(x => x.Subtotal);

            db.Compras.Add(compra);
            await db.SaveChangesAsync();

            foreach (var d in dto.Detalles)
            {
                var cant = d.Cantidad;
                var costo = d.PrecioCosto;
                var venta = Math.Round(costo * (1 + Margen), 2, MidpointRounding.AwayFromZero);

                // UPDATE atómico: existencia = existencia + cantidad (evita condiciones de carrera)
                await db.Productos.Where(p => p.IdProducto == d.IdProducto)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Existencia, p => p.Existencia + cant)
                        .SetProperty(p => p.PrecioCosto, costo)
                        .SetProperty(p => p.PrecioVenta, venta));
            }

            await tx.CommitAsync();

            var creada = await BuscarAsync(compra.IdCompra);
            return CreatedAtAction(nameof(Obtener), new { id = compra.IdCompra }, creada);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<CompraResponseDto?> BuscarAsync(int id)
    {
        var c = await db.Compras.AsNoTracking()
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(x => x.IdCompra == id);
        if (c is null) return null;

        return new CompraResponseDto(c.IdCompra, c.Fecha, c.IdProveedor, c.Proveedor!.Nombre, c.Total,
            c.Detalles.Select(d => new CompraDetalleDto(d.IdProducto, d.Producto!.Descripcion,
                d.Cantidad, d.PrecioCosto, d.Subtotal)).ToList());
    }
}
