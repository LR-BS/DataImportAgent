using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers
{
    public class DeviceMapper : ClassMap<Device>
    {
        public DeviceMapper()
        {
            Map(m => m.Id).Index(16);
            Map(m => m.DeviceNumber).Index(2);
            Map(m => m.DeviceType).Index(3).Default(-1);
            Map(m => m.Type).Index(4).TypeConverter<Int16Converter>();
            Map(m => m.AMM_Mode).Index(5).TypeConverter<Int16Converter>();
            Map(m => m.ArticleNumber).Index(6);
            Map(m => m.InstallmentLocation).Index(7);
            Map(m => m.DeviceSerialNumber).Index(8);
            Map(m => m.ModuleSeriaNumber).Index(9);
            Map(m => m.Scale).Index(10);
            Map(m => m.consumptionStartValue).Index(11).TypeConverter<DoubleConverter>().Default(0);
            Map(m => m.StartDate).Index(12);
            Map(m => m.EndDate).Index(13);
            Map(m => m.ConsumptionEndValue).Index(14).TypeConverter<DoubleConverter>().Default(0);
            Map(m => m.DevicePositionUUID).Index(15);
            Map(m => m.ConsumptionUnitId).Index(17);
            Map(m => m.Active).Index(18);
        }
    }
}