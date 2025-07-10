using Microsoft.AspNetCore.Mvc;
using PaymentSystem.Core.Services;

namespace PaymentSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    private readonly ICsvProcessorService _csvProcessor;
    private readonly ILogger<CsvController> _logger;

    public CsvController(ICsvProcessorService csvProcessor, ILogger<CsvController> logger)
    {
        _csvProcessor = csvProcessor;
        _logger = logger;
    }

    [HttpPost("procesar")]
    public async Task<IActionResult> ProcesarCsv([FromQuery] string archivo)
    {
        if (string.IsNullOrWhiteSpace(archivo))
            return BadRequest("Debes especificar la ruta del archivo de entrada.");

        if (!System.IO.File.Exists(archivo))
            return NotFound($"El archivo '{archivo}' no existe.");

        try
        {
            // Generar ruta de salida
            var directorio = Path.GetDirectoryName(archivo)!;
            var rutaSalida = Path.Combine(directorio, "salida.csv");

            _logger.LogInformation("Procesando archivo desde API...");
            _logger.LogInformation("Entrada: {Ruta}", archivo);
            _logger.LogInformation("Salida: {Ruta}", rutaSalida);

            await _csvProcessor.ProcesarCsvAsync(archivo);
            await _csvProcessor.GenerarResumenCsvAsync(rutaSalida);

            return Ok(new
            {
                mensaje = "Archivo procesado correctamente.",
                salida = rutaSalida
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar el CSV desde API.");
            return StatusCode(500, $"Error procesando CSV: {ex.Message}");
        }
    }
}
