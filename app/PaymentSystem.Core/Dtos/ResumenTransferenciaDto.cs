namespace PaymentSystem.Core.DTOs;

public class ResumenTransferenciaDto
{
    public string RutReceptor { get; set; } = string.Empty;
    public string NombreReceptor { get; set; } = string.Empty;
    public string NombreBanco { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string Moneda { get; set; } = string.Empty;
}
