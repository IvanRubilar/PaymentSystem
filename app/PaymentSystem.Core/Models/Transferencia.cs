namespace PaymentSystem.Core.Models;

public class Transferencia
{
    public int Id { get; set; }
    public string RutEmisor { get; set; } = string.Empty;
    public string NombreEmisor { get; set; } = string.Empty;
    public string IdTransaccion { get; set; } = string.Empty;
    public string RutReceptor { get; set; } = string.Empty;
    public string NombreReceptor { get; set; } = string.Empty;
    public string CodigoBanco { get; set; } = string.Empty;
    public string NombreBanco { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
