namespace PaymentSystem.Core.Services;

public interface ICsvProcessorService
{
    Task ProcesarCsvAsync(string rutaArchivo);
    Task GenerarResumenCsvAsync(string rutaSalida);
}
