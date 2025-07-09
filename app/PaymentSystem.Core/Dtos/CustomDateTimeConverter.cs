using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace PaymentSystem.Core.DTOs;

public class CustomDateTimeConverter : DateTimeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        if (DateTime.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }

        throw new FormatException($"Fecha inválida: {text}");
    }
}
