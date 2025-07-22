using CsvHelper.Configuration;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers
{
    public class DeviceCategoryMapper : ClassMap<DeviceCategory>
    {
        public DeviceCategoryMapper()
        {
            Map(m => m.DeviceType).Index(0);
            Map(m => m.ArticleNumber).Index(1);
            Map(m => m.Title).Index(2);
            Map(m => m.SapDescription).Index(3);
            Map(m => m.Unit).Index(4);
            Map(m => m.ExtrapolationCategory).Index(5);
            Map(m => m.Conversion).Index(6);
            Map(m => m.EnergyCarrierId).Index(7);
        }
    }
}