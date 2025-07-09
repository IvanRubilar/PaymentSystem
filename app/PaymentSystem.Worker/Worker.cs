using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PaymentSystem.Core.Services;

namespace PaymentSystem.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Iniciando ejecución automática de Worker");

    var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    var rutaEntrada = Path.Combine(baseDir, "archivos", "entrada.csv");
    var rutaSalida = Path.Combine(baseDir, "archivos", "salida.csv");

    _logger.LogInformation("Ruta entrada: {Ruta}", rutaEntrada);
    _logger.LogInformation("Ruta salida: {Ruta}", rutaSalida);

    using var scope = _serviceProvider.CreateScope();
    var csvProcessor = scope.ServiceProvider.GetRequiredService<ICsvProcessorService>();

    try
    {
        await csvProcessor.ProcesarCsvAsync(rutaEntrada);
        await csvProcessor.GenerarResumenCsvAsync(rutaSalida);
        _logger.LogInformation("Proceso finalizado correctamente.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error durante la ejecución del Worker");
    }
}

}
