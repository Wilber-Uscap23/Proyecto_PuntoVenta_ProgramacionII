using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuntoVentaAPI.Models;

// Complemento del modelo generado por la base de datos.
// No es necesario modificar ni sobrescribir Compra.cs.
public partial class Compra
{
    [Column("numero_factura", TypeName = "varchar(80)")]
    public string? NumeroFactura { get; set; }

    [Column("fecha_factura", TypeName = "date")]
    public DateOnly? FechaFactura { get; set; }

    [Column("factura_pdf", TypeName = "mediumblob")]
    public byte[]? FacturaPdf { get; set; }

    [Column("factura_nombre", TypeName = "varchar(180)")]
    public string? FacturaNombre { get; set; }
}
