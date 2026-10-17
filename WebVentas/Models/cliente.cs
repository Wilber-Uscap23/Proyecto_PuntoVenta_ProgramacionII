using System.ComponentModel.DataAnnotations;

namespace WebVentas.Models
{
    public class Cliente
    {
        public int id_cliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ][A-Za-zÁÉÍÓÚÜÑáéíóúüñ'’ .-]*$",
            ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        public string nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ][A-Za-zÁÉÍÓÚÜÑáéíóúüñ'’ .-]*$",
            ErrorMessage = "El apellido solo puede contener letras y espacios.")]
        public string apellido { get; set; } = "";

        [StringLength(20, ErrorMessage = "El NIT no puede pasar de 20 caracteres.")]
        [NitValido]
        public string? nit { get; set; }

        [RegularExpression(@"^(\+?502[ -]?)?[2-7]\d{3}[ -]?\d{4}$",
            ErrorMessage = "Teléfono inválido. Usa 8 dígitos que empiecen con 2 a 7 (ej. 5551 2345).")]
        public string? telefono { get; set; }

        [StringLength(150, ErrorMessage = "El correo no puede pasar de 150 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$",
            ErrorMessage = "El correo no tiene un formato válido (ej. nombre@correo.com).")]
        public string? correo { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no puede pasar de 255 caracteres.")]
        public string? direccion { get; set; }

        public DateTime fecha_registro { get; set; }
        public bool estado { get; set; } = true;

        // Limpia los espacios sobrantes antes de enviar los datos a la API.
        public void Normalizar()
        {
            nombre = Compactar(nombre);
            apellido = Compactar(apellido);
            nit = string.IsNullOrWhiteSpace(nit) ? null : nit.Trim();
            telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
            correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim();
            direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
        }

        private static string Compactar(string texto)
            => string.Join(' ', (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    // Valida el NIT de Guatemala: "CF" o un NIT con su dígito verificador correcto (módulo 11).
    [AttributeUsage(AttributeTargets.Property)]
    public class NitValidoAttribute : ValidationAttribute
    {
        public static bool EsValido(string nit)
        {
            var t = nit.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();
            if (t == "CF") return true;
            if (t.Length < 2 || t.Length > 13) return false;

            var cuerpo = t[..^1];
            var dv = t[^1];
            if (!cuerpo.All(char.IsAsciiDigit)) return false;
            if (!char.IsAsciiDigit(dv) && dv != 'K') return false;

            int factor = cuerpo.Length + 1, suma = 0;
            foreach (var c in cuerpo)
            {
                suma += (c - '0') * factor;
                factor--;
            }

            var modulo = (11 - (suma % 11)) % 11;
            var esperado = modulo == 10 ? 'K' : (char)('0' + modulo);
            return dv == esperado;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string texto || string.IsNullOrWhiteSpace(texto))
                return ValidationResult.Success;

            return EsValido(texto)
                ? ValidationResult.Success
                : new ValidationResult("NIT inválido. Escribe CF o un NIT con su dígito verificador correcto (ej. 3602978-5).");
        }
    }
}