using CsvHelper.Configuration;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers
{
    public class PropertyMapper : ClassMap<Property>
    {
        public PropertyMapper()
        {
            Map(m => m.Id).Index(10);
            Map(m => m.PropertyNumber).Index(0);
            Map(m => m.ExternalCode).Index(1);
            Map(m => m.PostCode).Index(2);
            Map(m => m.City).Index(3);
            Map(m => m.Street).Index(4);
            Map(m => m.Housenumber).Index(5);
            Map(m => m.Kaltmiete).Index(6);
            Map(m => m.IstaSpecialistId).Index(7);
            Map(m => m.PartnerCode).Index(8);
            //Map(m => m.PartnerUUID).Index(9);
            Map(m => m.Active).Index(11);
        }
    }
}