using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers
{
    public class ConsumptionUnitMapper : ClassMap<ConsumptionUnit>
    {
        public ConsumptionUnitMapper()
        {
            Map(m => m.Id).Index(18);
            Map(m => m.ConsumptionUnitNumber).Index(1);
            Map(m => m.Area).Index(3).TypeConverter<DoubleConverter>();
            Map(m => m.Street).Index(4);
            Map(m => m.HouseNnumber).Index(5);
            Map(m => m.Block).Index(6);
            Map(m => m.Staircase).Index(7);
            Map(m => m.Floor).Index(8);
            Map(m => m.Door).Index(9);
            Map(m => m.ConsumptionUnitType).Index(12);
            Map(m => m.IsMainMeter).Index(14);
            Map(m => m.CommonDwelling).Index(15);
            Map(m => m.PropertyId).Index(19);
            Map(m => m.Active).Index(20);
        }
    }
}