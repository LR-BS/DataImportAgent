using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Agents.ImportAgents;

public sealed class DeviceCategoryImportAgent : BaseImportAgent<DeviceCategory>
{
    public DeviceCategoryImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker) : base(dbContextFactory, _configuration, logger, _IFileWorker)
    {
    }

    public override void ImportFiles()
    {
        var Files = GetNonAccessedFiles(deviceCategoryFileKeyword);

        foreach (var DeviceCategoryFile in Files)
        {
            var fileHistory = CreateFileHistory(DeviceCategoryFile);

            try
            {
                var DeviceCategories = ReadFile(DeviceCategoryFile);

                DeviceCategories.ForEach(c =>
                {
                    c.AddFileHistory(fileHistory);
                });
                Db.Entry(fileHistory).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                StoreItems(DeviceCategories);
                MarkFileHistoryAsSuccess(fileHistory);
            }
            catch (Exception ex)
            {
                Db.Entry(fileHistory).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                LogFileReadingError(DeviceCategoryFile, ex);
                CreateFileHistoryWithError(DeviceCategoryFile, ex.Message);
            }
        }
    }

    public override List<DeviceCategory> ReadFile(string fileName)
    {
        var DeviceCategories = fileWorker.ReadFile<DeviceCategory, DeviceCategoryMapper>(fileName);
        return DeviceCategories.ToList();
    }

    public override void StoreItems(List<DeviceCategory> DeviceCategories)
    {
        Logger.LogInformation($"Validating {DeviceCategories.Count} items ");
        ValidateDeviceCategoryExistence(DeviceCategories);
        Logger.LogInformation($"Stroing {DeviceCategories.Count} items ");

        Db.DeviceCategories.AddRange(DeviceCategories);
        Db.SaveChanges();
    }

    private void ValidateDeviceCategoryExistence(List<DeviceCategory> importingDeviceCategories)
    {
        var errorList = new List<DeviceCategory>();

        var devices = Db.Devices.ToList();

        importingDeviceCategories.ForEach(deviceCategory =>
        {
            if (!devices.Any(a => a.DeviceType == deviceCategory.DeviceType && a.ArticleNumber == deviceCategory.ArticleNumber))
                errorList.Add(deviceCategory);
        });

        /////error when adding device category without having a device
        //if (errorList.Any())
        //{
        //    throw new ApplicationException($"Devices  with following Category  and articlenumbers are not found : \n " +
        //        $"{string.Join(',', errorList.Select(a => a.DeviceType.ToString() + "-" + a.ArticleNumber))} \n");
        //}

        errorList = Db.DeviceCategories.ToList().Join(importingDeviceCategories,
        a => new { a.DeviceType, a.ArticleNumber },
        b => new { b.DeviceType, b.ArticleNumber }, (a, b) => a).ToList();

        /// Error when duplicate device categories
        if (errorList.Any())
        {
            throw new ApplicationException($"Devices  with following Category  and articlenumbers are already existed : \n " +
                $"{string.Join(',', errorList.Select(a => a.DeviceType.ToString() + "-" + a.ArticleNumber))} \n");
        }
    }
}