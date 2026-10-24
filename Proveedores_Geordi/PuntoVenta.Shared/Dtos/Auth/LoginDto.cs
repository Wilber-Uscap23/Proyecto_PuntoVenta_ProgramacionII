// BASE DE INTEGRACIÓN (parte de Wilber: login + JWT).
// Se incluye solo para poder probar el menú y los reportes con un token real.
// Si Wilber ya tiene sus propios DTO de login, usen los de él y borren este archivo.
using System.ComponentModel.DataAnnotations;

namespace PuntoVenta.Shared.Dtos.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string NombreEmpleado { get; set; } = string.Empty;
}
