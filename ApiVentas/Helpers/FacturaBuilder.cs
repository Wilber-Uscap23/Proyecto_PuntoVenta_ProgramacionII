using ApiVentas.Models;

namespace ApiVentas.Helpers
{
    // Arma el documento de la factura (se usa para la vista previa y para la factura ya emitida).
    public static class FacturaBuilder
    {
        public static FacturaDocumento Construir(
            IConfiguration config,
            int numeroFel,
            DateTime fecha,
            string compradorNit,
            string compradorNombre,
            int idVenta,
            List<FacturaLinea> lineas,
            decimal total)
        {
            return new FacturaDocumento
            {
                numero_fel = numeroFel,
                serie = config["Empresa:Serie"] ?? "A",
                fecha_emision = fecha,

                emisor_nombre = config["Empresa:Nombre"] ?? "NOMBRE DE TU EMPRESA",
                emisor_nit = config["Empresa:Nit"] ?? "NIT DE TU EMPRESA",
                emisor_direccion = config["Empresa:Direccion"] ?? "DIRECCIÓN DE TU EMPRESA",

                comprador_nit = compradorNit,
                comprador_nombre = compradorNombre,

                id_venta = idVenta,

                Detalles = lineas,
                total_unidades = lineas.Sum(l => l.cantidad),
                descuento = 0m,
                total = total,
                total_letras = NumeroLetras.Convertir(total)
            };
        }
    }
}