namespace ApiProductos.Services;

/// <summary>
/// Guarda las imágenes de los productos en una carpeta del servidor (NO en la base de datos).
/// En MySQL solo se guarda la ruta, por ejemplo: /images/productos/3f2a....jpg
/// </summary>
public class AlmacenImagenes(string carpeta)
{
    public const long TamanoMaximo = 5 * 1024 * 1024; // 5 MB
    public const string RutaPublica = "/images/productos";

    private static readonly HashSet<string> Extensiones = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public async Task<(string? Url, string? Error)> GuardarAsync(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return (null, "Seleccione una imagen.");
        if (archivo.Length > TamanoMaximo)
            return (null, "La imagen no puede pesar más de 5 MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!Extensiones.Contains(extension))
            return (null, "Formato no permitido. Use JPG, PNG, GIF o WEBP.");

        // No basta con confiar en la extensión: se revisan los primeros bytes del archivo.
        var cabecera = new byte[12];
        await using (var lectura = archivo.OpenReadStream())
        {
            await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);
        }
        if (!EsImagen(cabecera))
            return (null, "El archivo no es una imagen válida.");

        var nombre = $"{Guid.NewGuid():N}{extension}";
        await using (var destino = new FileStream(Path.Combine(carpeta, nombre), FileMode.CreateNew))
        {
            await archivo.CopyToAsync(destino);
        }
        return ($"{RutaPublica}/{nombre}", null);
    }

    /// <summary>Elimina el archivo físico asociado a una ruta guardada en la base de datos.</summary>
    public void Eliminar(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(RutaPublica + "/", StringComparison.Ordinal))
            return;

        var ruta = Path.Combine(carpeta, Path.GetFileName(url)); // GetFileName evita rutas maliciosas (../)
        try { if (File.Exists(ruta)) File.Delete(ruta); }
        catch (IOException) { /* si está en uso, no se bloquea la operación principal */ }
    }

    private static bool EsImagen(byte[] b) =>
        (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)                                        // JPG
        || (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)                     // PNG
        || (b[0] == (byte)'G' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'8') // GIF
        || (b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
            && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P'); // WEBP
}
