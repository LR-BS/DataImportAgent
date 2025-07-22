using DataImportAgent.Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using WebnimbusDataImportAgent.Mappers;

namespace WebnimbusDataImportAgent
{
    public class ImportAgent
    {
        //todo - refactoring :: split this class into abstreacted child class
        private string? PropertyFileKeyword = "";

        private string? DeviceFileKeyword = "";
        private string? ConsumptionUnitFileKeyword = "";
        private string? deviceCategoryFileKeyword = "";
        private string? TemperatureFileKeyword = "";
        public IConfiguration configuration { get; }

        private IFileWorker fileWorker;
        public DataImportAgent.Logger.ILogger Logger;
        public string? webnumbusDirectory { get; }
        public VDMAdminDbContext Db { get; }

        public ImportAgent(VDMAdminDbContext _db, IConfiguration _configuration, IFileWorker _IFileWorker)
        {
            Db = _db;
            configuration = _configuration;
            fileWorker = _IFileWorker;

            PropertyFileKeyword = configuration["AgentSettings:PropertyFileKeyword"];
            DeviceFileKeyword = configuration["AgentSettings:DeviceFileKeyword"];
            ConsumptionUnitFileKeyword = configuration["AgentSettings:ConsumptionUnitKeyword"];
            TemperatureFileKeyword = configuration["AgentSettings:TemperatureFileKeyword"];
            deviceCategoryFileKeyword = configuration["AgentSettings:DeviceCategoryKeyword"];

            if (_configuration["Enviroment"] == "staging") webnumbusDirectory = configuration["AgentSettings:WebnimbusDirectoryStaging"];
            if (_configuration["Enviroment"] == "local") webnumbusDirectory = configuration["AgentSettings:WebnimbusDirectoryLocal"];
            if (_configuration["Enviroment"] == "production") webnumbusDirectory = configuration["AgentSettings:WebnimbusDirectoryProduction"];

            //check for missed Configurations
            if (string.IsNullOrEmpty(DeviceFileKeyword) || string.IsNullOrEmpty(DeviceFileKeyword) || string.IsNullOrEmpty(ConsumptionUnitFileKeyword))
            {
                throw new ApplicationException("Invalid Configuration set for AgentSettings files keyword . Empty values");
            }
        }

        public void ImportDeviceFiles()
        {
            Db.ChangeTracker.Clear();

            /// get Device Fies
            List<String> deviceFiles = fileWorker.GetDirectoryFiles(webnumbusDirectory, DeviceFileKeyword);

            List<string> accessed_files = Db.FileHistories.Where(f => f.FileName.Contains(DeviceFileKeyword)).Select(f => f.FileName).ToList();

            ///create a non-accessed file list
            if (accessed_files.Count > 0)
                deviceFiles = deviceFiles.Except(accessed_files).ToList();

            foreach (string deviceFile in deviceFiles)
            {
                try
                {
                    List<Device> devices = fileWorker.ReadFile<Device, DeviceMapper>(deviceFile);

                    FileHistory fileHistory = new FileHistory { FileName = deviceFile, JobStatus = FileHistory.Status.Success };

                    Db.FileHistories.Add(fileHistory);

                    ///update ImportFileID in bulk
                    devices.Select(c => { c.ImportFileID = fileHistory.Id; c.MigrationStatus = DeviceMigrationStatus.NOT_SET; return c; }).ToList();

                    devices.RemoveAll(a => a.Type == 2);
                    devices.RemoveAll(a => a.Type == 1 && a.AMM_Mode == 3);

                    Db.Devices.AddRange(devices);
                    Db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"File reading error in file  {deviceFile} ", ex);
                    var fileHistory = new FileHistory
                    {
                        FileName = deviceFile,
                        JobStatus = FileHistory.Status.Failed,
                        LogText = ex.Message
                    };

                    Db.FileHistories.Add(fileHistory);
                    Db.SaveChanges();
                }
            }
        }

        public void ImportTempratureFiles()
        {
            Db.ChangeTracker.Clear();

            List<String> temperatureFiles = fileWorker.GetDirectoryFiles(webnumbusDirectory, TemperatureFileKeyword);

            List<string> accessed_files = Db.FileHistories.Where(f => f.FileName.Contains(TemperatureFileKeyword)).Select(f => f.FileName).ToList();

            if (accessed_files.Count > 0)
                temperatureFiles = temperatureFiles.Except(accessed_files).ToList();

            foreach (string temperatureFile in temperatureFiles)
            {
                try
                {
                    List<Temperature> Temperatures = fileWorker.ReadFile<Temperature, TemperatureMapper>(temperatureFile);

                    FileHistory fileHistory = new FileHistory { FileName = temperatureFile, JobStatus = FileHistory.Status.Success };

                    Db.FileHistories.Add(fileHistory);

                    ///update ImportFileID in bulk
                    Temperatures.Select(c => { c.ImportFileID = fileHistory.Id; return c; }).ToList();

                    Db.Temperatures.AddRange(Temperatures);
                    Db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"File reading error in file  {temperatureFile} ", ex);
                    var fileHistory = new FileHistory
                    {
                        FileName = temperatureFile,
                        JobStatus = FileHistory.Status.Failed,
                        LogText = ex.Message
                    };

                    Db.FileHistories.Add(fileHistory);
                    Db.SaveChanges();
                }
            }
        }

        //public void ImportConsumptionUnitFiles()
        //{
        //    Db.ChangeTracker.Clear();

        //    /// get Device Fies
        //    List<String> consumptionUnitFiles = fileWorker.GetDirectoryFiles(webnumbusDirectory, ConsumptionUnitFileKeyword);

        //    List<string> accessed_files = Db.FileHistories.Where(f => f.FileName.Contains(ConsumptionUnitFileKeyword)).Select(f => f.FileName).ToList();

        //    ///create a non-accessed file list
        //    if (accessed_files.Count > 0)
        //        consumptionUnitFiles = consumptionUnitFiles.Except(accessed_files).ToList();

        //    var AllProperties = Db.Properties.Include(a => a.ConsumptionUnits).ToList();

        //    foreach (string consumptionUnitFile in consumptionUnitFiles)
        //    {
        //        try
        //        {
        //            //prepare list
        //            List<ConsumptionUnit> consumptionUnits = fileWorker.ReadWebnimbusFile<ConsumptionUnit, ConsumptionUnitMapper>(consumptionUnitFile);
        //            consumptionUnits.RemoveAll(a => a.ConsumptionUnitNumber == "9900");
        //            FileHistory fileHistory = new FileHistory { FileName = consumptionUnitFile, JobStatus = FileHistory.Status.Success };
        //            consumptionUnits = consumptionUnits.Select(c => { c.AddFileHistory(fileHistory); c.MigrationStatus = ConsumptionUnitMigrationStatus.NOT_SET; return c; }).ToList();

        //            //store list in corrosponding property
        //            consumptionUnits.GroupBy(a => a.PropertyUUID).ToList().ForEach(a =>
        //            {
        //                var property = AllProperties.Where(p => p.PropertyUUID == a.Key).FirstOrDefault();
        //                if (property == null) throw new ApplicationException($"property with Id {a.Key} is not exsited ");
        //                property.UpdateConsumptionUnits(a.ToList());
        //            });

        //            Db.SaveChanges();
        //        }
        //        catch (Exception ex)
        //        {
        //            Db.ChangeTracker.Clear();

        //            Logger.Log($"File reading error in file  {consumptionUnitFile} ", ex);
        //            FileHistory fileHistory = new FileHistory
        //            {
        //                FileName = consumptionUnitFile,
        //                JobStatus = FileHistory.Status.Failed,
        //                LogText = ex.Message
        //            };

        //            Db.FileHistories.Add(fileHistory);
        //            Db.SaveChanges();
        //        }
        //    }
        //}

        public void ImportDeviceCategoryFiles()
        {
            Db.ChangeTracker.Clear();

            /// get Device Fies
            List<String> deviceCategoryFiles = fileWorker.GetDirectoryFiles(webnumbusDirectory, deviceCategoryFileKeyword);

            List<string> accessed_files = Db.FileHistories.Where(f => f.FileName.Contains(deviceCategoryFileKeyword)).Select(f => f.FileName).ToList();

            ///create a non-accessed file list
            if (accessed_files.Count > 0)
                deviceCategoryFiles = deviceCategoryFiles.Except(accessed_files).ToList();

            foreach (string deviceCategoryFile in deviceCategoryFiles)
            {
                try
                {
                    List<DeviceCategory> deviceCategories = fileWorker.ReadFile<DeviceCategory, DeviceCategoryMapper>(deviceCategoryFile);

                    var fileHistory = new FileHistory { FileName = deviceCategoryFile, JobStatus = FileHistory.Status.Success };

                    Db.FileHistories.Add(fileHistory);

                    ///update ImportFileID in bulk
                    deviceCategories.Select(c => { c.ImportFileID = fileHistory.Id; return c; }).ToList();

                    Db.DeviceCategories.AddRange(deviceCategories);
                    Db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"File reading error in file  {deviceCategoryFile} ", ex);
                    FileHistory fileHistory = new FileHistory
                    {
                        FileName = deviceCategoryFile,
                        JobStatus = FileHistory.Status.Failed,
                        LogText = ex.Message
                    };

                    Db.FileHistories.Add(fileHistory);
                    Db.SaveChanges();
                }
            }
        }
    }
}