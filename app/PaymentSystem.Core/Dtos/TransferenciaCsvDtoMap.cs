using CsvHelper.Configuration;
using PaymentSystem.Core.DTOs;
using System.Globalization;

namespace PaymentSystem.Core.DTOs
{
    public sealed class TransferenciaCsvDtoMap : ClassMap<TransferenciaCsvDto>
    {
        public TransferenciaCsvDtoMap()
        {
            AutoMap(CultureInfo.InvariantCulture); 
            Map(m => m.RutEmisor).Index(0);
            Map(m => m.NombreEmisor).Index(1);
            Map(m => m.IdTransaccion).Index(2);
            Map(m => m.RutReceptor).Index(3);
            Map(m => m.NombreReceptor).Index(4);
            Map(m => m.CodigoBanco).Index(5);
            Map(m => m.NombreBanco).Index(6);
            Map(m => m.Monto).Index(7);
            Map(m => m.Moneda).Index(8);
            Map(m => m.Fecha).Index(9);
        }
    }
}
