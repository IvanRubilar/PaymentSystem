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
        var registros = csv.GetRecords<TransferenciaCsvDto>().ToList();

        var validos = registros
            .Where(r => !string.IsNullOrWhiteSpace(r.RutEmisor) &&
                        !string.IsNullOrWhiteSpace(r.NombreEmisor) &&
                        !string.IsNullOrWhiteSpace(r.IdTransaccion) &&
                        !string.IsNullOrWhiteSpace(r.RutReceptor) &&
                        !string.IsNullOrWhiteSpace(r.NombreReceptor) &&
                        !string.IsNullOrWhiteSpace(r.CodigoBanco) &&
                        !string.IsNullOrWhiteSpace(r.NombreBanco) &&
                        r.Monto > 0 &&
                        !string.IsNullOrWhiteSpace(r.Moneda))
            .Select(dto => new Transferencia
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
            })
            .ToList();

        _logger.LogInformation("Total registros válidos: {Count}", validos.Count);

        if (!validos.Any())
        {
            _logger.LogWarning("No se encontraron registros válidos para insertar.");
            return;
        }

        // Procesamiento en paralelo: insertar por chunks
        var tasks = validos
            .Chunk(100)
            .Select(async chunk =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                await scopedContext.Transferencias.AddRangeAsync(chunk);
                await scopedContext.SaveChangesAsync();
            });

        await Task.WhenAll(tasks);
        _logger.LogInformation("Inserción paralela completada.");
    }

public async Task GenerarResumenCsvAsync(string rutaSalida)
{
    using var scope = _serviceProvider.CreateScope();
    var scopedContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

    var transferencias = await scopedContext.Transferencias.ToListAsync();

    var resumen = transferencias
        .AsParallel()
        .Select(t => new
        {
            RutReceptor = t.RutReceptor,
            NombreReceptor = t.NombreReceptor,
            Banco = t.NombreBanco,
            MontoTotal = t.Monto,
            Moneda = t.Moneda
        })
        .ToList();

    using var writer = new StreamWriter(rutaSalida);
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    await csv.WriteRecordsAsync(resumen);
}

}
