// =====================================================================
//  BASE DE INTEGRACIÓN — el login con JWT es responsabilidad de Wilber.
//  Este controlador mínimo existe solo para poder probar el menú y los
//  reportes con un token real. Al integrar, usen el AuthController de
//  Wilber y borren este archivo.
// =====================================================================
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PuntoVenta.API.Data;
using PuntoVenta.API.Models;
using PuntoVenta.Shared.Dtos.Auth;
using PuntoVenta.Shared.Dtos.Reportes;

namespace PuntoVenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto, CancellationToken ct)
    {
        var usuario = await db.Usuarios
            .Include(u => u.Empleado)
            .FirstOrDefaultAsync(u => u.NombreUsuario == dto.Usuario && u.Estado, ct);

        if (usuario is null ||
            new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password)
                == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ErrorApiDto { Mensaje = "Usuario o contraseña incorrectos." });
        }

        usuario.UltimoAcceso = DateTime.Now;
        await db.SaveChangesAsync(ct);

        var expira = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:MinutosExpiracion", 120));
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Expires = expira,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario)
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                SecurityAlgorithms.HmacSha256)
        });

        return Ok(new LoginResponseDto
        {
            Token = token,
            Expira = expira,
            Usuario = usuario.NombreUsuario,
            NombreEmpleado = $"{usuario.Empleado?.Nombre} {usuario.Empleado?.Apellido}".Trim()
        });
    }
}
