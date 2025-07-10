using Microsoft.AspNetCore.Mvc;
using PaymentSystem.Core.Services;
using System.Diagnostics;

namespace PaymentSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    private readonly ICsvProcessorService _csvProcessor;
    private readonly ILogger<CsvController> _logger;
    private readonly IWebHostEnvironment _env;

    public CsvController(ICsvProcessorService csvProcessor, ILogger<CsvController> logger, IWebHostEnvironment env)
    {
        _csvProcessor = csvProcessor;
        _logger = logger;
        _env = env;
    }

    [HttpPost("procesar")]
    public async Task<IActionResult> ProcesarCsv([FromQuery] string nombreEntrada, [FromQuery] string nombreSalida)
    {
        if (string.IsNullOrWhiteSpace(nombreEntrada))
            return BadRequest("Debes especificar el nombre del archivo de entrada.");

        var baseDir = "/app";
        var carpeta = Path.Combine(baseDir, "archivos");
        var rutaEntrada = Path.Combine(carpeta, nombreEntrada);

        if (!System.IO.File.Exists(rutaEntrada))
            return NotFound($"El archivo '{nombreEntrada}' no existe en la carpeta 'archivos'.'{rutaEntrada}'");

        var rutaSalida = string.IsNullOrWhiteSpace(nombreSalida)
            ? Path.Combine(carpeta, $"NOMINA.PAGOS.{DateTime.Now:yyyyMMdd}.csv")
            : Path.Combine(carpeta, nombreSalida);

        // ==== INICIO GENERACIÓN LOG ====
        var logDir = Path.Combine(carpeta, "LOG");
        Directory.CreateDirectory(logDir);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var rutaLog = Path.Combine(logDir, $"EJECUCION.{timestamp}.LOG");

        try
        {
            using var logStream = new StreamWriter(rutaLog, append: false);
            logStream.WriteLine($"===== EJECUCIÓN API - {DateTime.Now} =====");
            logStream.WriteLine($"Archivo de entrada : {rutaEntrada}");
            logStream.WriteLine($"Archivo de salida  : {rutaSalida}");
            logStream.WriteLine($"Hilos disponibles  : {Environment.ProcessorCount}");
            logStream.WriteLine($"Hora de inicio     : {DateTime.Now:HH:mm:ss}");

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Procesando archivo desde API...");
            _logger.LogInformation("Entrada: {Ruta}", rutaEntrada);
            _logger.LogInformation("Salida: {Ruta}", rutaSalida);

            // Conectar hook de log
            CsvProcessorService.OnLogInfo = mensaje =>
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                logStream.WriteLine($"[INFO {timestamp}] {mensaje}");
            };

            await _csvProcessor.ProcesarCsvAsync(rutaEntrada);
            await _csvProcessor.GenerarResumenCsvAsync(rutaSalida);

            stopwatch.Stop();

            logStream.WriteLine($"Hora de término    : {DateTime.Now:HH:mm:ss}");
            logStream.WriteLine($"Duración total     : {stopwatch.ElapsedMilliseconds} ms");
            logStream.WriteLine("Resultado           : EJECUCIÓN EXITOSA");

            _logger.LogInformation("Tiempo total de ejecución desde API: {Tiempo} ms", stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                mensaje = "Archivo procesado correctamente.",
                salida = rutaSalida
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar el CSV desde API.");

            await System.IO.File.WriteAllTextAsync(rutaLog, $"ERROR FATAL: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, $"Error procesando CSV: {ex.Message}");
        }
    }
}
