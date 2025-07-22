using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Agents.ImportAgents;

public sealed class PropertyImportAgent : BaseImportAgent<Property>
{
    public PropertyImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker) : base(dbContextFactory, _configuration, logger, _IFileWorker)
    {
    }

    private void ValidateProperty(Property property)
    {
        if (string.IsNullOrEmpty(property.PartnerCode))
        {
            throw new ApplicationException($"Property with ID {property.Id} has no partnerCode");
        }
        if(string.IsNullOrEmpty(property.PropertyNumber))
        {
            throw new ApplicationException($"Property with ID {property.Id} has no PropertyNumber");
        }

    }
    
    public override void ImportFiles()
    {
        
        var Files = GetNonAccessedFiles(PropertyFileKeyword);

        foreach (var propertyFile in Files)
        {
            var fileHistory = CreateFileHistory(propertyFile);

            try
            {
                var properties = ReadFile(propertyFile);

                properties.ForEach(c =>
                {
                    ValidateProperty(c);
                    c.AddFileHistory(fileHistory);
                    c.MigrationStatus = PropertyMigrationStatus.NOT_SET;
                });
                Db.Entry(fileHistory).State = EntityState.Added;
                StoreItems(properties);
                MarkFileHistoryAsSuccess(fileHistory);
            }
            catch (Exception ex)
            {
                Db.Entry(fileHistory).State = EntityState.Detached;
                LogFileReadingError(propertyFile, ex);
                CreateFileHistoryWithError(propertyFile, ex.Message);
            }
        }
    }

    public override List<Property> ReadFile(string fileName)
    {
        var properties = fileWorker.ReadFile<Property, PropertyMapper>(fileName);
        return properties.ToList();
    }

    public override void StoreItems(List<Property> properties)
    {
        
        HandlePropertyDeleteion(properties.Where(a => a.Active == false).ToList());

        properties.RemoveAll(a => a.Active == false);

        var existedProperties = Db.Properties.Where(a => properties.Select(a => a.Id).Contains(a.Id)).ToList();

        existedProperties.ForEach(property =>
        {
            if (property.ContractNumber == "5")
            {
                throw new ApplicationException($"Demounit cannot be updated. Property with ID {property.Id} has ContractNumber 5");
            }
            var editedProperty = properties.Find(a => a.Id == property.Id)!;

            if (property.ManuallyUpdated) { return; } // skip properties updated from webportal

            if (editedProperty.Equals(property)) { return; } ///skip the same existed item

            property.Active = editedProperty.Active;
            property.ExternalCode = editedProperty.ExternalCode;

            property.PostCode = editedProperty.PostCode;
            property.City = editedProperty.City;
            property.Street = editedProperty.Street;
            property.Housenumber = editedProperty.Housenumber;
            property.Kaltmiete = editedProperty.Kaltmiete;
            property.PartnerCode = editedProperty.PartnerCode;
            property.Active = editedProperty.Active;
            property.ImportedFile = editedProperty.ImportedFile;

            if (property.MigrationStatus == PropertyMigrationStatus.SENT_TO_WP || property.MigrationStatus == PropertyMigrationStatus.ASSIGNED_TO_PARTNER)
            {
                property.MigrationStatus = PropertyMigrationStatus.EDITED;
            }
        });

        properties.RemoveAll(a => existedProperties.Select(a => a.Id).Contains(a.Id));
        Db.Properties.AddRange(properties);
        Db.SaveChanges();
    }

    public void HandlePropertyDeleteion(List<Property> properties)
    {
        if (properties.Count == 0) return;

        properties.ForEach(property =>
        {
            var existedProperty = Db.Properties.FirstOrDefault(a => a.Id == property.Id);
            if (existedProperty == null) { return; }
            var consumptionUnits = Db.ConsumptionUnits.Where(a => a.PropertyId == property.Id).ToList();
            consumptionUnits.ForEach(a =>
            {
                a.MigrationStatus = ConsumptionUnitMigrationStatus.PLANNED_TO_BE_DELETED;
                Db.SaveChanges();
                
                var devices = Db.Devices.Where(b => b.ConsumptionUnitId == a.Id).ToList();
                devices.ForEach(b =>
                {
                    b.MigrationStatus = DeviceMigrationStatus.PLANNED_TO_BE_DELETED;
                    Db.SaveChanges();
                    Db.Entry(b).State = EntityState.Detached;
                });
                Db.Entry(a).State = EntityState.Detached;
            });
            
            existedProperty.MigrationStatus = PropertyMigrationStatus.REQUESTED_TO_BE_DELETED;
            Db.SaveChanges();
            //Db.Entry(existedProperty).State = EntityState.Detached;

        });

        var importedFile = properties.First().ImportedFile;
        if (Db.FileHistories.Where(a => a.FileName == importedFile.FileName).Any() == false)
            Db.FileHistories.Add(importedFile);
        Db.SaveChanges();
    }
}