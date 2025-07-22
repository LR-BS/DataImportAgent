using SharedKernel.Domain;

namespace DataImportAgent;

public interface IGPSHelper
{
    public (double, double) GetGeolocationData(Property property);
}