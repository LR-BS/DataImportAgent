using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Agents.ImportAgents;

public sealed class ConsumptionUnitImportAgent : BaseImportAgent<ConsumptionUnit>
{
    public ConsumptionUnitImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker) : base(dbContextFactory, _configuration, logger, _IFileWorker)
    {
    }

    public ConsumptionUnit ValidateConsumptionUnit(ConsumptionUnit unit)
    {
        // Pflichtdaten:
        //CU Nr, Area, Street, Haunummer,
        // CU Type, PropertyID, Stockwerk (wird auf
        // 0 Gesetzt wenn leer
        
        if (string.IsNullOrEmpty(unit.ConsumptionUnitNumber))
        {
            throw new ApplicationException($"ConsumptionUnit with ID {unit.Id} has no ConsumptionUnitNumber");
        }
        if (string.IsNullOrEmpty(unit.Street))
        {
            throw new ApplicationException($"ConsumptionUnit with ID {unit.Id} has no Street");
        }
        if (string.IsNullOrEmpty(unit.HouseNnumber))
        {
            throw new ApplicationException($"ConsumptionUnit with ID {unit.Id} has no HouseNumber");
        }
        if(string.IsNullOrEmpty(unit.Floor))
        {
            unit.Floor = "0";
        }
        
        if(unit.Area is > 4000 or < 0)
        {
            throw new Exception($"Area is invalid for consumption unit {unit.Id} with area {unit.Area}");
        }
        
        

        return unit;
    }
    
    public void ValidateConsumptionUnitTenants(Tenant tenant)
    {
        if (string.IsNullOrEmpty(tenant.Name))
        {
            throw new ApplicationException($"Tenant with ID {tenant.Id} has no Name");
        }

        if (tenant.MoveOutDate != null)
        {
            if(tenant.MoveOutDate < tenant.MoveInDate)
            {
                throw new ApplicationException($"Tenant with ID {tenant.Id} has a move out date before the move in date");
            }
        }
    }
    public override void ImportFiles()
    {
        var consumptionUnitFiles = GetNonAccessedFiles(ConsumptionUnitFileKeyword);

        foreach (var consumptionUnitFile in consumptionUnitFiles)
        {
            var fileHistory = CreateFileHistory(consumptionUnitFile);

            try
            {
                var consumptionUnits = ReadFile(consumptionUnitFile);

                consumptionUnits.ForEach(c =>
                {
                    c.AddFileHistory(fileHistory);
                    c.MigrationStatus = ConsumptionUnitMigrationStatus.NOT_SET;
                });
                //Db.SaveChanges();
                Db.Entry(fileHistory).State = EntityState.Added;
                StoreItems(consumptionUnits);
                MarkFileHistoryAsSuccess(fileHistory);
            }
            catch (Exception ex)
            {
                Db.Entry(fileHistory).State = EntityState.Detached;
                Logger.LogError("Error while reading file",ex);
                //LogFileReadingError(consumptionUnitFile, ex);
                CreateFileHistoryWithError(consumptionUnitFile, ex.Message);
            }
        }
    }

    public override List<ConsumptionUnit> ReadFile(string fileName)
    {
        var inputConsumptionUnits = fileWorker.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(fileName);
        var tenants = fileWorker.ReadFile<Tenant, TenantMapper>(fileName);

        var uniqueIds = inputConsumptionUnits.Select(a => a.Id).Distinct().ToList();
            
        var consumptionUnits = new List<ConsumptionUnit>();

        foreach (var id in uniqueIds)
        {
            var consumptionUnitItem = inputConsumptionUnits.Find(a => a.Id == id);
            consumptionUnitItem = ValidateConsumptionUnit(consumptionUnitItem);
            var consumptionUnitTenants = tenants.Where(a => a.ConsumptionUnitId == id).ToList();
            Logger.LogInformation("Tenants" + consumptionUnitTenants.Count);
            foreach (var tenant in consumptionUnitTenants)
            {
                ValidateConsumptionUnitTenants(tenant);
                    tenant.MigrationStatus = TenantMigrationStatus.NOT_SET;
                    consumptionUnitItem!.Tenants.Add(tenant);
            }
            Logger.LogInformation($"ConsumptionUnit {id} has {consumptionUnitItem!.Tenants.Count} tenants");
            consumptionUnits.Add(consumptionUnitItem!);
        }
        
        return consumptionUnits.Where(c => c.ConsumptionUnitNumber != "9900").ToList();
    }

    public override void StoreItems(List<ConsumptionUnit> consumptionUnits)
    {
        try
        {
            Logger.LogInformation($"Validating {consumptionUnits.Count} items ");
            ValidatePropertyExistence(consumptionUnits);
            Logger.LogInformation($"Saving {consumptionUnits.Count} items ");

            var newConsumptionUnits = new List<ConsumptionUnit>();

            HandleConsumptionUnitDeleteion(consumptionUnits.Where(a => a.Active == false).ToList());

            consumptionUnits.RemoveAll(a => a.Active == false);
            var tenants = new List<Tenant>();

            consumptionUnits.ForEach(item =>
            {
                
                tenants.AddRange(item.Tenants);
                item.Tenants.Clear();
                var existedItem = Db.ConsumptionUnits.FirstOrDefault(a => a.Id == item.Id); //check for existing in db

                if (existedItem is not null)
                {
                    if (existedItem.Equals(item)) return; //skip the same existing item

                    existedItem.Area = item.Area;
                    existedItem.HouseNnumber = item.HouseNnumber;
                    existedItem.Block = item.Block;
                    existedItem.Staircase = item.Staircase;
                    existedItem.Floor = item.Floor;
                    existedItem.Street = item.Street;
                    existedItem.ConsumptionUnitNumber = item.ConsumptionUnitNumber;
                    existedItem.Door = item.Door;
                    existedItem.ConsumptionUnitType = item.ConsumptionUnitType;
                    existedItem.IsMainMeter = item.IsMainMeter;
                    existedItem.CommonDwelling = item.CommonDwelling;
                    existedItem.ImportedFile = item.ImportedFile;
                    existedItem.PropertyId = item.PropertyId;
                    if (existedItem.MigrationStatus == ConsumptionUnitMigrationStatus.SENT_TO_WP)
                    {
                        existedItem.MigrationStatus = ConsumptionUnitMigrationStatus.EDITED;
                    }
                    else
                    {
                        existedItem.MigrationStatus = ConsumptionUnitMigrationStatus.NOT_SET;
                    }
                }
                else
                {
                    newConsumptionUnits.Add(item);

                }
            });
            Db.ConsumptionUnits.AddRange(newConsumptionUnits);
            Db.SaveChanges();

            var existedConsumptionUnits = Db.ConsumptionUnits.Select(a => a.Id).ToList();

            tenants.ForEach(tenant =>
            {
                var existedTenant = Db.Tenants.Where(a => a.Id == tenant.Id).FirstOrDefault();

                if (existedConsumptionUnits.Contains(tenant.ConsumptionUnitId) == false)
                    throw new Exception(
                        $"Tenant cannot be saved . consumptionUnit with Id {tenant.Id} couldn't be found ");

                if ((existedTenant is null))
                {
                    Logger.LogInformation("Tenant doesn't exist" + tenant.Id);
                    Db.Tenants.Add(tenant);
                }
                else
                {
                    Logger.LogInformation("Tenant exists" + tenant.Id);
                    Logger.LogInformation("Tenant moveoutDate: " + tenant.MoveOutDate);
                    Logger.LogInformation("Tenant moveoutDate in db: " + existedTenant.MoveOutDate);
                    
                    //if (existedTenant.Equals(tenant)) return; //skip the same existing item

                    if (tenant.MoveOutDate == existedTenant.MoveOutDate)
                    {
                        Logger.LogInformation("Tenant not updated" + tenant.Id);
                        return; /// tenant is same as before and we do not update the status
                    }
                    
                    existedTenant.MoveOutDate = tenant.MoveOutDate;
                    existedTenant.MigrationStatus = TenantMigrationStatus.PREPARED_FOR_UPDATE_TO_WP;
                    Logger.LogInformation("Tenant moveoutdate updated" + tenant.Id);
                    Db.Tenants.Update(existedTenant); 
                    
                }
            });

            Db.SaveChanges();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while saving consumption units", ex);
            throw;
        }
    }

    public void HandleConsumptionUnitDeleteion(List<ConsumptionUnit> consumptionUnits)
    {
        try
        {
            if (consumptionUnits.Count == 0) return;
            consumptionUnits.ForEach(consumptionUnit =>
            {
                var existedconsumptionUnit = Db.ConsumptionUnits.FirstOrDefault(a => a.Id == consumptionUnit.Id);
                if (existedconsumptionUnit == null)
                {
                    return;
                }
                var devices = Db.Devices.Where(a => a.ConsumptionUnitId == consumptionUnit.Id).ToList();
                devices.ForEach(device =>
                {
                    //device.MigrationStatus = DeviceMigrationStatus.PLANNED_TO_BE_DELETED;
                    //Db.SaveChanges();
                    Db.Entry(device).State = EntityState.Detached;
                });

                existedconsumptionUnit.MigrationStatus = ConsumptionUnitMigrationStatus.PLANNED_TO_BE_DELETED;
                Db.SaveChanges();
                Db.Entry(existedconsumptionUnit).State = EntityState.Detached;
            });
            var importedFile = consumptionUnits.First().ImportedFile;
            if (Db.FileHistories.Any(a => a.FileName == importedFile.FileName) == false)
                Db.FileHistories.Add(importedFile);
            Db.SaveChanges();
        } catch (Exception ex)
        {
            Logger.LogError("Error while handling consumption unit deletion", ex);
            throw;
        }
    }

    public void ValidatePropertyExistence(List<ConsumptionUnit> consumptionUnits)
    {
        var propertyUuids = consumptionUnits.Select(c => c.PropertyId).Distinct().ToList();
        var properties = Db.Properties.Where(p => propertyUuids.Contains(p.Id)).ToList();

        foreach (var consumptionUnit in consumptionUnits)
        {
            var matchingProperty = properties.FirstOrDefault(p => p.Id == consumptionUnit.PropertyId);
            
            if (matchingProperty == null)
            {
                throw new Exception($"No matching Property found for ConsumptionUnit with PropertyUuid: {consumptionUnit.PropertyId}");
            }

            if (matchingProperty.ContractNumber == "5")
            {
                throw new Exception($"Import Data contains a ConsumptionUnit with PropertyUuid: {consumptionUnit.PropertyId} which is a DemoUnit");
            }
            //consumptionUnit.Property = matchingProperty;
            
        }
    }
}