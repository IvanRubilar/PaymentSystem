using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace PaymentSystem.Core.DTOs;

public class TransferenciaCsvDto
{
    [Index(0)]
    public string RutEmisor { get; set; } = string.Empty;

    [Index(1)]
    public string NombreEmisor { get; set; } = string.Empty;

    [Index(2)]
    public string IdTransaccion { get; set; } = string.Empty;

    [Index(3)]
    public string RutReceptor { get; set; } = string.Empty;

    [Index(4)]
    public string NombreReceptor { get; set; } = string.Empty;

    [Index(5)]
    public string CodigoBanco { get; set; } = string.Empty;

    [Index(6)]
    public string NombreBanco { get; set; } = string.Empty;

    [Index(7)]
    public decimal Monto { get; set; }

    [Index(8)]
    public string Moneda { get; set; } = string.Empty;

    [Index(9)]
    
    [TypeConverter(typeof(CustomDateTimeConverter))]
    public DateTime Fecha { get; set; }
}
