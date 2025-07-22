using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DataImportAgent.PropertyEnrichment;
using SharedKernel.Domain;

namespace WebnimbusDataImportAgent.Mappers;

public class PropertyEnrichmentMapper : ClassMap<PropertyEnrichmentInformation>
{
    public PropertyEnrichmentMapper()
    {
        Map(m => m.PropertyNumber).Index(0);
        Map(m => m.ContractNumber).Index(1);
        Map(m => m.StartDate).Index(2);
    }
}