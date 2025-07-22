using DataImportAgent.Agents;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using WebnimbusDataImportAgent.Mappers;
using WebnimbusDataImportAgent;
using System.Collections.Generic;
using DataImportAgent.Agents.ImportAgents;
using Microsoft.EntityFrameworkCore;

namespace DataImportAgent.PropertyEnrichment;

public sealed class PropertyEnrichmentAgent : BaseImportAgent<PropertyEnrichmentInformation>
{
    private readonly IGPSHelper gpsHelper;

    public PropertyEnrichmentAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker, IGPSHelper _GpsHelper) : base(dbContextFactory, _configuration, logger, _IFileWorker)
    {
        gpsHelper = _GpsHelper;
    }

    public override void ImportFiles()
    {
        var propertyEnrichmentFiles = GetNonAccessedFiles(PropertyEnrichmentFileKeyword);
        foreach (var propertyEnrichmentFile in propertyEnrichmentFiles)
        {
            try
            {
                var enrichmentItems = ReadFile(propertyEnrichmentFile);
                var fileHistory = CreateFileHistory(propertyEnrichmentFile);
                Db.Entry(fileHistory).State = Microsoft.EntityFrameworkCore.EntityState.Added;

                enrichmentItems.ForEach(enrichmentDetails =>
                {
                    var property = Db.Properties.Where(a => a.PropertyNumber == enrichmentDetails.PropertyNumber).FirstOrDefault();

                    if (property == null)
                        throw new ApplicationException($"Property with propertyNumber {enrichmentDetails.PropertyNumber} is not found ");

                    (double latitude, double longitude) = gpsHelper.GetGeolocationData(property);

                    property.StartDate = enrichmentDetails.StartDate;
                    property.ContractNumber = enrichmentDetails.ContractNumber;
                    property.MigrationStatus = PropertyMigrationStatus.DONE_ENRICHMENT;
                    property.GPSLatitude = latitude;
                    property.GPSLongitude = longitude;
                    property.AddFileHistory(fileHistory);
                });
                Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Db.ChangeTracker.Clear();
                LogFileReadingError(propertyEnrichmentFile, ex);
                CreateFileHistoryWithError(propertyEnrichmentFile, ex.Message);
            }
        }
    }

    public override List<PropertyEnrichmentInformation> ReadFile(string fileName)
    {
        var propertyEnrichmentDetails = fileWorker.ReadFile<PropertyEnrichmentInformation, PropertyEnrichmentMapper>(fileName);
        return propertyEnrichmentDetails;
    }

    public override void StoreItems(List<PropertyEnrichmentInformation> consumptionUnits)
    {
        throw new NotImplementedException();
        //no need for this funciton
    }
}