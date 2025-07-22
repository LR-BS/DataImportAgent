using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Agents.ImportAgents;

public sealed class DeviceImportAgent : BaseImportAgent<Device>
{
    public DeviceImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker) : base(dbContextFactory, _configuration, logger, _IFileWorker)
    {
    }

    public void ValidateDevice(Device device)
    {
        // Pflichtdaten:
        // Geräteart, Artikelnummer, Einbauort,
        // Montagedatum, GerätepositionUUID,
        // ConsumtionUnit_UUID
        if (device.DeviceType == 0)
        {
            Logger.LogInformation($"Device with ID {device.Id} has device type 0");
        }
        if (string.IsNullOrEmpty(device.ArticleNumber))
        {
            throw new ApplicationException($"Device with ID {device.Id} has no article number");
        }
        if (string.IsNullOrEmpty(device.InstallmentLocation))
        {
            throw new ApplicationException($"Device with ID {device.Id} has no installment location");
        }
        
        
        if (device.StartDate == DateTime.MinValue || device.StartDate == null)
        {
            throw new ApplicationException($"Device with ID {device.Id} has no start date");
        }
        if(device.EndDate != null && device.EndDate < device.StartDate)
        {
            throw new ApplicationException($"Device with ID {device.Id} has an end date before the start date");
        }
        
        if(device.ConsumptionEndValue > 0 && device.ConsumptionEndValue < device.consumptionStartValue)
        {
            throw new ApplicationException($"Device with ID {device.Id} has an end value before the start value");
        }
        
    }

    public override void ImportFiles()
    {
        
        var Files = GetNonAccessedFiles(DeviceFileKeyword);

        foreach (var DeviceFile in Files)
        {
            var fileHistory = CreateFileHistory(DeviceFile);
            
            try
            {
                var devices = ReadFile(DeviceFile);
                devices.RemoveAll(a => a.Type == 2);
                devices.RemoveAll(a => a.Type == 1 && a.AMM_Mode == 3);

                devices.ForEach(c =>
                {
                    c.AddFileHistory(fileHistory);
                    c.MigrationStatus = DeviceMigrationStatus.NOT_SET;
                });

                ValidateDuplicateDevices(devices);
                Db.Entry(fileHistory).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                StoreItems(devices);
                MarkFileHistoryAsSuccess(fileHistory);
            }
            catch (Exception ex)
            {
                Db.Entry(fileHistory).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                LogFileReadingError(DeviceFile, ex);
                CreateFileHistoryWithError(DeviceFile, ex.Message);
            }
        }
    }

    public static void ValidateDuplicateDevices(List<Device> list)
    {
        var knownKeys = new HashSet<Guid>();
        var duplicated = new HashSet<Guid>();

        list.ForEach(item =>
        {
            if (knownKeys.Add(item.Id) == false) duplicated.Add(item.Id);
        });

        if (duplicated.Any())
        {
            throw new ApplicationException($"Duplicate deviceUUID found  with ID : [{string.Join(",", duplicated)}]");
        }
    }

    public override List<Device> ReadFile(string fileName)
    {
        var devices = fileWorker.ReadFile<Device, DeviceMapper>(fileName);
        return devices.ToList();
    }

    /// <summary>
    /// Store devices based on requirements in VDMA-597
    /// </summary>
    /// <param name="devices"></param>
    public override void StoreItems(List<Device> devices)
    {
        Logger.LogInformation($"Validating {devices.Count} items ");

        var deletedItems = devices.Where(a => a.Active == false).ToList();
        if (deletedItems.Any())
        {
            Logger.LogInformation($"{deletedItems.Count} unactive devices found  ");
            HandleDevicesDeleteion(deletedItems);
            devices.RemoveAll(a => a.Active == false);
        }

        ValidateConsumptionUnitExistence(devices);
        Logger.LogInformation($"Saving {devices.Count} items ");

        List<Device> deviceList = new();
        devices.ForEach(item =>
        {
            ValidateDevice(item);
            var existedDevice = Db.Devices.FirstOrDefault(d => d.Id == item.Id);

            var existedPosition = Db.Devices.FirstOrDefault(d => d.DevicePositionUUID == item.DevicePositionUUID);

            if (existedDevice == null)
            {
                //new device
                item.MigrationStatus = DeviceMigrationStatus.NOT_SET;

                if (existedPosition != null)
                {
                    //todo add test for this condition
                    item.MigrationStatus = DeviceMigrationStatus.PREPARED_FOR_UPDTE_WP;

                    if (item.EndDate is not null)
                    {
                        //for new devices with end Date we store but don't send it to WP
                        item.MigrationStatus = DeviceMigrationStatus.REPLACED_AND_NOT_SENT;
                    }
                }

                deviceList.Add(item);
                return;
            }

            if (existedDevice.Equals(item)) return; // skip device with the same information

            //existing device - update fields
            existedDevice.DeviceNumber = item.DeviceNumber;
            existedDevice.DeviceType = item.DeviceType;
            existedDevice.Type = item.Type;
            existedDevice.AMM_Mode = item.AMM_Mode;
            existedDevice.ArticleNumber = item.ArticleNumber;
            existedDevice.InstallmentLocation = item.InstallmentLocation;
            existedDevice.DeviceSerialNumber = item.DeviceSerialNumber;
            existedDevice.ModuleSeriaNumber = item.ModuleSeriaNumber;
            existedDevice.Scale = item.Scale;
            existedDevice.StartDate = item.StartDate;
            existedDevice.EndDate = item.EndDate;
            existedDevice.consumptionStartValue = item.consumptionStartValue;
            existedDevice.ConsumptionEndValue = item.ConsumptionEndValue;
            existedDevice.DevicePositionUUID = item.DevicePositionUUID;
            existedDevice.ConsumptionUnitId = item.ConsumptionUnitId;
            existedDevice.Active = item.Active;
            existedDevice.ImportedFile = item.ImportedFile;
            if (item.EndDate == null)
            {
                existedDevice.MigrationStatus = DeviceMigrationStatus.PREPARED_FOR_UPDTE_WP;
            }
            else
            {
                //we have a device change
                existedDevice.MigrationStatus = DeviceMigrationStatus.REPLACED_AND_NOT_SENT;
            }
        });

        Db.Devices.AddRange(deviceList);
        Db.SaveChanges();
    }

    public void HandleDevicesDeleteion(List<Device> devices)
    {
        if (devices.Count == 0) return;

        devices.ForEach(device =>
        {
            var existedDevice = Db.Devices.Where(a => a.Id == device.Id).FirstOrDefault();

            if (existedDevice == null) { return; }

            
            existedDevice.MigrationStatus = DeviceMigrationStatus.PLANNED_TO_BE_DELETED;
            Db.SaveChanges();
            Db.Entry(existedDevice).State = EntityState.Detached;
        });
        var importedFile = devices.First().ImportedFile;
        if (Db.FileHistories.Where(a => a.FileName == importedFile.FileName).Any() == false)
            Db.FileHistories.Add(importedFile);

        Db.SaveChanges();
    }

    private void ValidateConsumptionUnitExistence(List<Device> devices)
    {
        var consumptionUnitUUIDs = devices.Select(c => c.ConsumptionUnitId).Distinct().ToList();
        var existingUUIDs = Db.ConsumptionUnits.Select(p => p.Id).ToList();
        var unmatchedUUIDs = consumptionUnitUUIDs.Except(existingUUIDs).ToList();

        if (unmatchedUUIDs.Any())
        {
            throw new ApplicationException($"Devices with the following ConsumptionUnitUUIDs were not found in ConsumptionUnits: {string.Join(", ", unmatchedUUIDs)}");
        }
    }
}