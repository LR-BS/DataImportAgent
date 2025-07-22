namespace DataImportAgent.PropertyEnrichment;

public record PropertyEnrichmentInformation
{
    public string? ContractNumber { get; set; }
    public double? GPSLatitude { get; set; }
    public double? GPSLongitude { get; set; }
    public string PropertyNumber { get; set; }
    public DateTime StartDate { get; set; }
}

