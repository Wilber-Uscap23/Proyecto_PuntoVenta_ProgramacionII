using ApiVentas.Models;

namespace ApiVentas.Helpers
{
    // Reglas de facturación del proyecto.
    public static class ReglasFactura
    {
        // Las ventas MAYORES a este monto no se pueden facturar a consumidor final.
        public const decimal LimiteConsumidorFinal = 2500m;

        public static bool EsConsumidorFinal(string? nit)
            => string.IsNullOrWhiteSpace(nit) || nit.Trim().Equals("CF", StringComparison.OrdinalIgnoreCase);

        // NIT que se imprime en la factura ("CF" si el cliente no tiene).
        public static string NitComprador(Cliente cliente)
            => EsConsumidorFinal(cliente.nit) ? "CF" : cliente.nit!.Trim();

        // Devuelve el mensaje de error si la venta no cumple la regla, o null si todo está bien.
        public static string? Validar(decimal total, Cliente cliente)
        {
            if (total > LimiteConsumidorFinal && EsConsumidorFinal(cliente.nit))
            {
                return $"Las ventas mayores a Q {LimiteConsumidorFinal:N2} no se pueden facturar a consumidor final. " +
                       $"El cliente \"{cliente.nombre} {cliente.apellido}\" no tiene NIT; usa un cliente con NIT.";
            }
            return null;
        }
    }
}