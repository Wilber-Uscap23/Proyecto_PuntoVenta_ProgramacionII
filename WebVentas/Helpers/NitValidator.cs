namespace WebVentas.Helpers
{
    // Misma regla que usa la API en Models/cliente.cs: "CF" o NIT con dígito verificador correcto (módulo 11).
    public static class NitValidator
    {
        public static string Normalizar(string? nit) =>
            (nit ?? "").Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();

        // 3602978-5  (o CF)
        public static string Formatear(string? nit)
        {
            var t = Normalizar(nit);
            if (t == "CF" || t.Length < 2) return t;
            return t[..^1] + "-" + t[^1];
        }

        public static bool EsValido(string? nit)
        {
            var t = Normalizar(nit);
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
    }
}