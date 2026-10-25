using Microsoft.EntityFrameworkCore;
using PuntoVentaAPI.Models;

namespace PuntoVentaAPI.Data;

public partial class PuntoVentaDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            // Rol del usuario
            entity.Property(u => u.Rol)
                .HasMaxLength(20)
                .HasDefaultValue("VENDEDOR")
                .HasColumnName("rol");

            // Correo empresarial como nombre de usuario
            entity.Property(u => u.Usuario1)
                .HasMaxLength(150)
                .HasColumnName("usuario");

            // Cambio obligatorio de contraseña
            entity.Property(u => u.DebeCambiarContrasena)
                .HasDefaultValue(false)
                .HasColumnName("debe_cambiar_contrasena");
        });
    }
}