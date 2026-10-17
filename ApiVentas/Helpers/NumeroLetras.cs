namespace ApiVentas.Helpers
{
    // Convierte un monto a letras, por ejemplo:
    // 282.85 -> "DOSCIENTOS OCHENTA Y DOS CON 85/100 QUETZALES"
    public static class NumeroLetras
    {
        private static readonly string[] Unidades =
        {
            "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ",
            "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE",
            "VEINTE", "VEINTIUNO", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO", "VEINTICINCO",
            "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
        };

        private static readonly string[] Decenas =
        {
            "", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
        };

        private static readonly string[] Centenas =
        {
            "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
            "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

        public static string Convertir(decimal monto)
        {
            var entero = (long)Math.Truncate(monto);
            var centavos = (int)Math.Round((monto - entero) * 100, MidpointRounding.AwayFromZero);
            if (centavos == 100)
            {
                entero++;
                centavos = 0;
            }

            if (entero >= 1_000_000_000)
                return $"{entero:N0} CON {centavos:00}/100 QUETZALES";

            var letras = entero == 0 ? "CERO" : Enteros(entero);
            return $"{letras} CON {centavos:00}/100 QUETZALES";
        }

        private static string Enteros(long n)
        {
            var partes = new List<string>();
            var millones = (int)(n / 1_000_000);
            var miles = (int)((n % 1_000_000) / 1000);
            var resto = (int)(n % 1000);

            if (millones > 0)
                partes.Add(millones == 1 ? "UN MILLÓN" : Apocope(Menor1000(millones)) + " MILLONES");
            if (miles > 0)
                partes.Add(miles == 1 ? "MIL" : Apocope(Menor1000(miles)) + " MIL");
            if (resto > 0)
                partes.Add(Menor1000(resto));

            return string.Join(" ", partes);
        }

        private static string Menor1000(int n)
        {
            if (n == 100) return "CIEN";

            var c = n / 100;
            var r = n % 100;
            var partes = new List<string>();

            if (c > 0) partes.Add(Centenas[c]);
            if (r > 0)
            {
                if (r < 30)
                {
                    partes.Add(Unidades[r]);
                }
                else
                {
                    var d = r / 10;
                    var u = r % 10;
                    partes.Add(u == 0 ? Decenas[d] : $"{Decenas[d]} Y {Unidades[u]}");
                }
            }

            return string.Join(" ", partes);
        }

        // "UNO" -> "UN", "VEINTIUNO" -> "VEINTIÚN" (cuando va antes de "MIL" o "MILLONES")
        private static string Apocope(string s)
        {
            if (s.EndsWith("VEINTIUNO")) return s[..^9] + "VEINTIÚN";
            if (s.EndsWith("UNO")) return s[..^1];
            return s;
        }
    }
}