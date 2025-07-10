using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PaymentSystem.Core.Services;
using System.Diagnostics;

namespace PaymentSystem.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool modoDebug = _configuration.GetValue<bool>("ModoDebug");

        if (modoDebug)
        {
            _logger.LogWarning("ModoDebug activado: ejecutando proceso inmediatamente (solo una vez para pruebas).");
            await EjecutarProcesoAsync();
            return;
        }

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
                return;
            }

            _logger.LogInformation("Ejecutando procesamiento automático a las 23:00 (Chile)");

            await EjecutarProcesoAsync();
        }
    }

  private async Task EjecutarProcesoAsync()
{
    try
    {
        var fechaActual = DateTime.Now.ToString("yyyyMMdd");
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var baseDir = "/app";

        var rutaEntrada = Path.Combine(baseDir, "archivos", "REGISTRO.TRX.csv");
        var rutaSalida = Path.Combine(baseDir, "archivos", $"NOMINA.PAGOS.{fechaActual}.csv");
        var logDir = Path.Combine(baseDir, "archivos", "LOG");
        Directory.CreateDirectory(logDir);
        var rutaLog = Path.Combine(logDir, $"EJECUCION.{timestamp}.LOG");

        using var logStream = new StreamWriter(rutaLog, append: false);
        logStream.WriteLine($"===== INICIO EJECUCIÓN WORKER - {DateTime.Now} =====");
        logStream.WriteLine($"Archivo de entrada : {rutaEntrada}");
        logStream.WriteLine($"Archivo de salida  : {rutaSalida}");
        logStream.WriteLine($"Hilos disponibles  : {Environment.ProcessorCount}");
        logStream.WriteLine($"Hora de inicio     : {DateTime.Now:HH:mm:ss}");

        using var scope = _serviceProvider.CreateScope();
        var csvProcessor = scope.ServiceProvider.GetRequiredService<ICsvProcessorService>();

        // Conectar log interno del servicio al archivo log del worker
        CsvProcessorService.OnLogInfo = mensaje =>
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            logStream.WriteLine($"[INFO {timestamp}] {mensaje}");
        };

        var stopwatch = Stopwatch.StartNew();

        await csvProcessor.ProcesarCsvAsync(rutaEntrada);
        await csvProcessor.GenerarResumenCsvAsync(rutaSalida);

        stopwatch.Stop();
        logStream.WriteLine($"Hora de término    : {DateTime.Now:HH:mm:ss}");
        logStream.WriteLine($"Duración total     : {stopwatch.ElapsedMilliseconds} ms");
        logStream.WriteLine("Resultado           : EJECUCIÓN EXITOSA");

        _logger.LogInformation("Proceso finalizado correctamente en {Tiempo} ms", stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error durante la ejecución del Worker");

        var fechaError = DateTime.Now.ToString("yyyyMMddHHmmss");
        var logDir = Path.Combine(AppContext.BaseDirectory, "archivos", "LOG");
        var rutaLog = Path.Combine(logDir, $"EJECUCION.{fechaError}.LOG");

        Directory.CreateDirectory(logDir);
        await File.WriteAllTextAsync(rutaLog, $"ERROR FATAL: {ex.Message}\n{ex.StackTrace}");
    }
}
}
