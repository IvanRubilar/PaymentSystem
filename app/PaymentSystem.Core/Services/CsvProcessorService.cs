using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentSystem.Core.Db;
using PaymentSystem.Core.DTOs;
using PaymentSystem.Core.Models;
using System.Globalization;

namespace PaymentSystem.Core.Services;

public class CsvProcessorService : ICsvProcessorService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<CsvProcessorService> _logger;

    public CsvProcessorService(PaymentDbContext context, ILogger<CsvProcessorService> logger)
    {
        _context = context;
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
            }).ToList();

        _logger.LogInformation("Registros válidos: {Count}", validos.Count);

        if (validos.Count > 0)
        {
            await _context.Transferencias.AddRangeAsync(validos);
            await _context.SaveChangesAsync();
        }
    }

    public async Task GenerarResumenCsvAsync(string rutaSalida)
    {
        var transferencias = await _context.Transferencias.ToListAsync();

        var resumenes = transferencias
            .AsParallel()
            .GroupBy(t => new { t.RutReceptor, t.NombreReceptor, t.NombreBanco, t.Moneda })
            .Select(g => new ResumenTransferenciaDto
            {
                RutReceptor = g.Key.RutReceptor,
                NombreReceptor = g.Key.NombreReceptor,
                NombreBanco = g.Key.NombreBanco,
                Moneda = g.Key.Moneda,
                MontoTotal = g.Sum(t => t.Monto)
            })
            .ToList();

        using var writer = new StreamWriter(rutaSalida);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        await csv.WriteRecordsAsync(resumenes);

        _logger.LogInformation("Archivo de resumen generado en: {Path}", rutaSalida);
    }
}
