using CsvHelper.Configuration;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers;

public class TenantMapper : ClassMap<Tenant>
{
    public TenantMapper()
    {
        Map(m => m.Name).Index(2);
        Map(m => m.MoveInDate).Index(10);
        Map(m => m.MoveOutDate).Index(11);
        Map(m => m.ConsumptionUnitId).Index(18);
        Map(m => m.VacantDwelling).Index(16);
        Map(m => m.Id).Index(17);
        Map(m => m.ExternalTenantId).Index(13);
    }
}