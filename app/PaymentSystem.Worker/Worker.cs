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
        _logger.LogInformation("Worker iniciado. Esperando para ejecutar diariamente a las 23:00 hora de Chile.");

        var chileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var nowChile = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, chileTimeZone);

            var nextRunChile = nowChile.Date.AddHours(23);
            if (nowChile > nextRunChile)
                nextRunChile = nextRunChile.AddDays(1);

            var delay = nextRunChile - nowChile;
            _logger.LogInformation("Próxima ejecución programada en: {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Finaliza si se cancela el token
                return;
            }

            _logger.LogInformation("Ejecutando procesamiento automático a las 23:00 (Chile)");

            try
            {
                var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
                var rutaEntrada = Path.Combine(baseDir, "archivos", "entrada.csv");
                var rutaSalida = Path.Combine(baseDir, "archivos", "salida.csv");

                _logger.LogInformation("Ruta entrada: {Ruta}", rutaEntrada);
                _logger.LogInformation("Ruta salida: {Ruta}", rutaSalida);

                using var scope = _serviceProvider.CreateScope();
                var csvProcessor = scope.ServiceProvider.GetRequiredService<ICsvProcessorService>();

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
}
