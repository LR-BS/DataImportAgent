using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Agents.ImportAgents;

public sealed class TemperatureImportAgent : BaseImportAgent<Temperature>
{
    public TemperatureImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker) : base(dbContextFactory, _configuration, logger, _IFileWorker)
    {
    }

    public override void ImportFiles()
    {
        var Files = GetNonAccessedFiles(TemperatureFileKeyword);

        foreach (var TemperatureFile in Files)
        {
            var fileHistory = CreateFileHistory(TemperatureFile);

            try
            {
                var Temperatures = ReadFile(TemperatureFile);

                Temperatures.ForEach(c =>
                {
                    c.AddFileHistory(fileHistory);
                });
                Db.Entry(fileHistory).State = EntityState.Added;
                StoreItems(Temperatures);
                MarkFileHistoryAsSuccess(fileHistory);
            }
            catch (Exception ex)
            {
                Db.Entry(fileHistory).State = EntityState.Detached;
                LogFileReadingError(TemperatureFile, ex);
                CreateFileHistoryWithError(TemperatureFile, ex.Message);
            }
        }
    }

    public override List<Temperature> ReadFile(string fileName)
    {
        var Temperatures = fileWorker.ReadFile<Temperature, TemperatureMapper>(fileName);
        return Temperatures.ToList();
    }

    public override void StoreItems(List<Temperature> Temperatures)
    {
        Logger.LogInformation($"Validating {Temperatures.Count} items ");
        ValidateTemperatureExistence(Temperatures);
        Logger.LogInformation($"Saving {Temperatures.Count} items ");

        Db.Temperatures.AddRange(Temperatures);
        Db.SaveChanges();
    }

    private void ValidateTemperatureExistence(List<Temperature> importingTemperatures)
    {
        var errorList = new List<Temperature>();

        errorList = Db.Temperatures.ToList().Join(importingTemperatures,
        a => new { a.Date, a.ProvinceCode },
        b => new { b.Date, b.ProvinceCode }, (a, b) => a).ToList();

        /// Error when duplicate temperature categories
        if (errorList.Any())
        {
            throw new ApplicationException($"Tempearture  with following state  and date are already existed : \n " +
                $"{string.Join(',', errorList.Select(a => a.Date.ToString()))}");
        }
    }
}