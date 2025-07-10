using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using PaymentSystem.Core.Db;
using PaymentSystem.Core.DTOs;
using PaymentSystem.Core.Models;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace PaymentSystem.Core.Services;

public class CsvProcessorService : ICsvProcessorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CsvProcessorService> _logger;

    // Hook para log externo
    public static Action<string>? OnLogInfo { get; set; }

    public CsvProcessorService(IServiceProvider serviceProvider, ILogger<CsvProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

public async Task ProcesarCsvAsync(string rutaArchivo)
{
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        MissingFieldFound = null,
        BadDataFound = null
    };

    using var reader = new StreamReader(rutaArchivo);
    using var csv = new CsvReader(reader, config);
    csv.Context.RegisterClassMap<TransferenciaCsvDtoMap>();
    var registros = csv.GetRecords<TransferenciaCsvDto>().ToList();

    var validos = new List<Transferencia>();
    var invalidos = new List<TransferenciaCsvDto>();

    foreach (var dto in registros)
    {
        bool esValido =
            !string.IsNullOrWhiteSpace(dto.RutEmisor) &&
            !string.IsNullOrWhiteSpace(dto.NombreEmisor) &&
            !string.IsNullOrWhiteSpace(dto.IdTransaccion) &&
            !string.IsNullOrWhiteSpace(dto.RutReceptor) &&
            !string.IsNullOrWhiteSpace(dto.NombreReceptor) &&
            !string.IsNullOrWhiteSpace(dto.CodigoBanco) &&
            !string.IsNullOrWhiteSpace(dto.NombreBanco) &&
            dto.Monto > 0 &&
            !string.IsNullOrWhiteSpace(dto.Moneda);

        if (esValido)
        {
            validos.Add(new Transferencia
            {
                RutEmisor = dto.RutEmisor,
                NombreEmisor = dto.NombreEmisor,
                IdTransaccion = dto.IdTransaccion,
                RutReceptor = dto.RutReceptor,
                NombreReceptor = dto.NombreReceptor,
                CodigoBanco = dto.CodigoBanco,
                NombreBanco = dto.NombreBanco,
                Monto = dto.Monto,
                Moneda = dto.Moneda,
                Fecha = DateTime.SpecifyKind(dto.Fecha, DateTimeKind.Utc)
            });
        }
        else
        {
            invalidos.Add(dto);
        }
    }

    _logger.LogInformation("Total registros válidos iniciales: {Count}", validos.Count);
    OnLogInfo?.Invoke($"Total registros válidos iniciales: {validos.Count}");
    OnLogInfo?.Invoke($"Total registros inválidos: {invalidos.Count}");

    var baseDir = "/app";

    // Duplicados en archivo
    var idsRepetidosArchivo = validos
        .GroupBy(t => t.IdTransaccion)
        .Where(g => g.Count() > 1)
        .SelectMany(g => g.Skip(1)) // omitir el primero válido
        .ToList();

    if (idsRepetidosArchivo.Any())
    {
        OnLogInfo?.Invoke($"Se encontraron {idsRepetidosArchivo.Count} duplicados en el archivo y serán ignorados.");
        validos = validos
            .GroupBy(t => t.IdTransaccion)
            .Select(g => g.First()) // eliminar duplicados
            .ToList();
    }

    // Duplicados en base de datos
    using var scopeCheck = _serviceProvider.CreateScope();
    var dbCheck = scopeCheck.ServiceProvider.GetRequiredService<PaymentDbContext>();

    var idsCsv = validos.Select(v => v.IdTransaccion).ToList();
    var idsEnBd = await dbCheck.Transferencias
        .Where(t => idsCsv.Contains(t.IdTransaccion))
        .Select(t => t.IdTransaccion)
        .ToListAsync();

    if (idsEnBd.Any())
    {
        OnLogInfo?.Invoke($"Se ignorarán {idsEnBd.Count} registros porque ya existen en la base de datos.");
        validos = validos
            .Where(t => !idsEnBd.Contains(t.IdTransaccion))
            .ToList();
    }

    // Escribir registros inválidos o ignorados por duplicados
    var registrosIgnorados = registros
        .Where(dto =>
            invalidos.Contains(dto) ||
            idsRepetidosArchivo.Any(d => d.IdTransaccion == dto.IdTransaccion) ||
            idsEnBd.Contains(dto.IdTransaccion)
        )
        .ToList();

    if (registrosIgnorados.Any())
    {
        var erroresDir = Path.Combine(baseDir, "archivos");
        var erroresArchivo = Path.Combine(erroresDir, $"ERRORES.TRX.{DateTime.Now:yyyyMMddHHmmss}.csv");

        using var writer = new StreamWriter(erroresArchivo);
        using var erroresCsv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        erroresCsv.Context.RegisterClassMap<TransferenciaCsvDtoMap>();
        await erroresCsv.WriteRecordsAsync(registrosIgnorados);

        OnLogInfo?.Invoke($"Archivo de errores generado: {erroresArchivo}");
    }

    if (!validos.Any())
    {
        _logger.LogWarning("No hay registros únicos válidos para insertar.");
        OnLogInfo?.Invoke("No hay registros únicos válidos para insertar.");
        return;
    }

    int chunkSize = 100;
    var chunks = validos.Chunk(chunkSize).ToList();
    int threadCount = Environment.ProcessorCount;

    OnLogInfo?.Invoke($"Procesando en paralelo con {threadCount} hilos.");
    OnLogInfo?.Invoke($"Insertando {validos.Count} registros en {chunks.Count} chunks.");

    var tasks = chunks.Select(async chunk =>
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await scopedContext.Transferencias.AddRangeAsync(chunk);
        await scopedContext.SaveChangesAsync();
    });

    await Task.WhenAll(tasks);

    _logger.LogInformation("Inserción paralela completada.");
    OnLogInfo?.Invoke("Inserción paralela completada.");
}




    public async Task GenerarResumenCsvAsync(string rutaSalida)
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        var transferencias = await scopedContext.Transferencias.ToListAsync();

        OnLogInfo?.Invoke($"Total de transferencias a resumir: {transferencias.Count}");

        var resumen = transferencias
            .AsParallel()
            .GroupBy(t => new
            {
                t.RutReceptor,
                t.NombreReceptor,
                t.NombreBanco,
                t.Moneda
            })
            .Select(g => new
            {
                RutReceptor = g.Key.RutReceptor,
                NombreReceptor = g.Key.NombreReceptor,
                Banco = g.Key.NombreBanco,
                MontoTotal = g.Sum(t => t.Monto),
                Moneda = g.Key.Moneda
            })
            .OrderBy(r => r.Banco)
            .ThenBy(r => r.Moneda)
            .ToList();

        OnLogInfo?.Invoke($"Resumen generado con {resumen.Count} líneas agrupadas.");
        _logger.LogInformation("Archivo de resumen generado.");

        using var writer = new StreamWriter(rutaSalida);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(resumen);
    }
}
