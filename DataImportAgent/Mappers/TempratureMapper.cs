using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers
{
    public class TemperatureMapper : ClassMap<Temperature>
    {
        public TemperatureMapper()
        {
            Map(m => m.Value).Index(2);
            Map(m => m.Date).Index(1);
            Map(m => m.ProvinceCode).Index(0);
        }
    }
}