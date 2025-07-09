using Microsoft.AspNetCore.Mvc;
using PaymentSystem.API.Services;

namespace PaymentSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    private readonly ICsvProcessorService _csvProcessor;

    public CsvController(ICsvProcessorService csvProcessor)
    {
        _csvProcessor = csvProcessor;
    }

    [HttpPost("procesar")]
    public async Task<IActionResult> ProcesarCsv([FromQuery] string archivo)
    {
        if (!System.IO.File.Exists(archivo))
        {
            return NotFound($"El archivo {archivo} no existe.");
        }

        try
        {
            await _csvProcessor.ProcesarCsvAsync(archivo);
            return Ok("Archivo procesado correctamente.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error procesando CSV: {ex.Message}");
        }
    }
}
