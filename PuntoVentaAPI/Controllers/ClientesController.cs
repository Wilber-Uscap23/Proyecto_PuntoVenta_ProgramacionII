using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PuntoVentaAPI.Data;
using PuntoVentaAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PuntoVentaAPI.Controllers;

// MODELO PARA REGISTRAR Y EDITAR CLIENTES
// Se conservan las validaciones originales.

public class GuardarClienteDto : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    [RegularExpression(@"^[\p{L}]+(?:[ '\-][\p{L}]+)*$",
        ErrorMessage = "El nombre solo puede contener letras, espacios, guiones y apóstrofes.")]
    public string Nombre { get; set; } = string.Empty;
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
    [RegularExpression(@"^[\p{L}]+(?:[ '\-][\p{L}]+)*$",
        ErrorMessage = "El apellido solo puede contener letras, espacios, guiones y apóstrofes.")]
    public string Apellido { get; set; } = string.Empty;
    [StringLength(20)]
    public string? Nit { get; set; }
    [StringLength(20)]
    public string? Telefono { get; set; }
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(150)]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
        ErrorMessage = "Ingresa un correo con formato válido.")]
    public string? Correo { get; set; }
    [StringLength(255)]
    public string? Direccion { get; set; }
    // VALIDACIONES ADICIONALES
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        // Consumidor Final es el registro genérico usado por Ventas.
        // Todos los demás clientes deben tener NIT, correo y dirección.
        bool esConsumidorFinal =
            string.Equals(Nombre.Trim(), "Consumidor", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Apellido.Trim(), "Final", StringComparison.OrdinalIgnoreCase);

        if (!esConsumidorFinal)
        {
            if (string.IsNullOrWhiteSpace(Nit))
                yield return new ValidationResult(
                    "El NIT es obligatorio.", new[] { nameof(Nit) });
            if (string.IsNullOrWhiteSpace(Correo))
                yield return new ValidationResult(
                    "El correo electrónico es obligatorio.", new[] { nameof(Correo) });
            if (string.IsNullOrWhiteSpace(Direccion))
                yield return new ValidationResult(
                    "La dirección es obligatoria.", new[] { nameof(Direccion) });
        }

        if (!string.IsNullOrWhiteSpace(Nit) && !NitValido(Nit))
        {
            yield return new ValidationResult(
                "El NIT debe contener solo números y tener de 8 a 10 dígitos.",
                new[] { nameof(Nit) });
        }
        if (!string.IsNullOrWhiteSpace(Telefono))
        {
            bool telefonoValido = Regex.IsMatch(
                Telefono.Trim(),
                @"^(?:\+502[ -]?)?[2-7][0-9]{3}[ -]?[0-9]{4}$");
            if (!telefonoValido)
            {
                yield return new ValidationResult(
                    "Ingresa un teléfono guatemalteco de 8 dígitos. Ejemplo: 5555-1234.",
                    new[] { nameof(Telefono) });
            }
        }
        if (!string.IsNullOrWhiteSpace(Direccion))
        {
            string direccion = Direccion.Trim();
            if (direccion.Length < 5 ||
                !direccion.Any(char.IsLetter))
            {
                yield return new ValidationResult(
                    "La dirección debe tener mínimo 5 caracteres e incluir información descriptiva.",
                    new[] { nameof(Direccion) });
            }
        }
    }
    // NIT numérico de 8 a 10 dígitos, sin verificador.
    private static bool NitValido(string nit)
    {
        return Regex.IsMatch(nit.Trim(), @"^[0-9]{8,10}$");
    }

}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,VENDEDOR,CONSULTA")]
public class ClientesController : ControllerBase
{
    private readonly PuntoVentaDbContext _context;
    public ClientesController(PuntoVentaDbContext context)
    {
        _context = context;
    }

    // ========================================
    // CONSULTAR TODOS LOS CLIENTES

    // ========================================
    [HttpGet]
    public async Task<IActionResult> ObtenerClientes()
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .OrderBy(c => c.IdCliente)
            .Select(c => new
            {
                c.IdCliente,
                c.Nombre,
                c.Apellido,
                c.Nit,
                c.Telefono,
                c.Correo,
                c.Direccion,
                c.FechaRegistro,
                c.Estado
            })
            .ToListAsync();
        return Ok(clientes);
    }

    // ========================================
    // CONSULTAR CLIENTE POR ID

    // ========================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerClientePorId(int id)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.IdCliente == id)
            .Select(c => new
            {
                c.IdCliente,
                c.Nombre,
                c.Apellido,
                c.Nit,
                c.Telefono,
                c.Correo,
                c.Direccion,
                c.FechaRegistro,
                c.Estado
            })
            .FirstOrDefaultAsync();
        if (cliente == null)
        {
            return NotFound(new
            {
                mensaje = "Cliente no encontrado."
            });
        }
        return Ok(cliente);
    }

    // ========================================
    // REGISTRAR CLIENTE

    // ========================================
    [HttpPost]
    [Authorize(Roles = "ADMIN,VENDEDOR")]
    public async Task<IActionResult> RegistrarCliente(
        [FromBody] GuardarClienteDto datos)
    {
        string nombre = datos.Nombre.Trim();
        string apellido = datos.Apellido.Trim();
        string? nit = Limpiar(datos.Nit)?.ToUpperInvariant();
        string? correo = Limpiar(datos.Correo)?.ToLowerInvariant();
        if (string.Equals(nombre, "Consumidor", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(apellido, "Final", StringComparison.OrdinalIgnoreCase) &&
            await _context.Clientes.AnyAsync(c =>
                c.Nombre == "Consumidor" && c.Apellido == "Final"))
        {
            return Conflict(new
            {
                mensaje = "Consumidor Final ya está registrado; utilizá el registro existente."
            });
        }
        if (await NitDuplicado(nit))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un cliente con ese NIT."
            });
        }
        if (await CorreoDuplicado(correo))
        {
            return Conflict(new
            {
                mensaje = "Ya existe un cliente con ese correo."
            });
        }
        var cliente = new Cliente
        {
            Nombre = nombre,
            Apellido = apellido,
            Nit = nit,
            Telefono = Limpiar(datos.Telefono),
            Correo = correo,
            Direccion = Limpiar(datos.Direccion),
            Estado = true
        };
        _context.Clientes.Add(cliente);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            return Conflict(new
            {
                mensaje = "El NIT o correo ya está registrado."
            });
        }
        return CreatedAtAction(
            nameof(ObtenerClientePorId),
            new { id = cliente.IdCliente },
            new
            {
                mensaje = "Cliente registrado correctamente.",
                cliente.IdCliente
            });
    }

    // ========================================
    // ACTUALIZAR CLIENTE

    // ========================================
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActualizarCliente(
        int id,
        [FromBody] GuardarClienteDto datos)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.IdCliente == id);
        if (cliente == null)
        {
            return NotFound(new
            {
                mensaje = "Cliente no encontrado."
            });
        }
        string nombre = datos.Nombre.Trim();
        string apellido = datos.Apellido.Trim();
        string? nit = Limpiar(datos.Nit)?.ToUpperInvariant();
        string? correo = Limpiar(datos.Correo)?.ToLowerInvariant();
        if (await NitDuplicado(nit, id))
        {
            return Conflict(new
            {
                mensaje = "El NIT pertenece a otro cliente."
            });
        }
        if (await CorreoDuplicado(correo, id))
        {
            return Conflict(new
            {
                mensaje = "El correo pertenece a otro cliente."
            });
        }
        cliente.Nombre = nombre;
        cliente.Apellido = apellido;
        cliente.Nit = nit;
        cliente.Telefono = Limpiar(datos.Telefono);
        cliente.Correo = correo;
        cliente.Direccion = Limpiar(datos.Direccion);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsDuplicado(ex))
        {
            return Conflict(new
            {
                mensaje = "El NIT o correo ya está registrado."
            });
        }
        return Ok(new
        {
            mensaje = "Cliente actualizado correctamente.",
            cliente.IdCliente,
            cliente.Nombre,
            cliente.Apellido,
            cliente.Nit,
            cliente.Telefono,
            cliente.Correo,
            cliente.Direccion,
            cliente.FechaRegistro,
            cliente.Estado
        });
    }

    // ========================================
    // DESACTIVAR CLIENTE

    // ========================================
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DesactivarCliente(int id)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.IdCliente == id);
        if (cliente == null)
        {
            return NotFound(new
            {
                mensaje = "Cliente no encontrado."
            });
        }
        if (cliente.Estado != true)
        {
            return BadRequest(new
            {
                mensaje = "El cliente ya está inactivo."
            });
        }
        cliente.Estado = false;
        await _context.SaveChangesAsync();
        return Ok(new
        {
            mensaje = "Cliente desactivado correctamente.",
            cliente.IdCliente,
            cliente.Estado
        });
    }

    // ========================================
    // REACTIVAR CLIENTE

    // ========================================
    [HttpPut("{id:int}/activar")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActivarCliente(int id)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.IdCliente == id);
        if (cliente == null)
        {
            return NotFound(new
            {
                mensaje = "Cliente no encontrado."
            });
        }
        if (cliente.Estado == true)
        {
            return BadRequest(new
            {
                mensaje = "El cliente ya está activo."
            });
        }
        cliente.Estado = true;
        await _context.SaveChangesAsync();
        return Ok(new
        {
            mensaje = "Cliente activado correctamente.",
            cliente.IdCliente,
            cliente.Estado
        });
    }

    // ========================================
    // COMPROBAR NIT REPETIDO

    // ========================================
    private async Task<bool> NitDuplicado(
        string? nit, int? excluirId = null)
    {
        if (string.IsNullOrWhiteSpace(nit))
            return false;

        // También detecta NIT antiguos guardados con guion.
        string nitConGuion = nit.Length > 1
            ? nit.Insert(nit.Length - 1, "-")
            : nit;

        return await _context.Clientes.AnyAsync(c =>
            (c.Nit == nit || c.Nit == nitConGuion) &&
            (!excluirId.HasValue || c.IdCliente != excluirId.Value));
    }
    // ========================================
    // COMPROBAR CORREO REPETIDO

    // ========================================
    private async Task<bool> CorreoDuplicado(
        string? correo, int? excluirId = null)
    {
        if (correo == null)
            return false;
        return await _context.Clientes.AnyAsync(c =>
            c.Correo == correo &&
            (!excluirId.HasValue || c.IdCliente != excluirId.Value));
    }

    // ========================================
    // LIMPIAR CAMPOS OPCIONALES

    // ========================================
    private static string? Limpiar(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    // ========================================
    // IDENTIFICAR DUPLICADOS MYSQL

    // ========================================
    private static bool EsDuplicado(DbUpdateException error)
    {
        return error.InnerException is MySqlException mysql &&
               mysql.Number == 1062;
    }
}
