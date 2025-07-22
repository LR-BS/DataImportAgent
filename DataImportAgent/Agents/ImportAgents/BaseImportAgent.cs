using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using WebnimbusDataImportAgent;

namespace DataImportAgent.Agents.ImportAgents;

public abstract class BaseImportAgent<T> where T : class
{
    protected string PropertyFileKeyword = "";

    protected readonly string DeviceFileKeyword = "";
    protected readonly string ConsumptionUnitFileKeyword = "";
    protected readonly string deviceCategoryFileKeyword = "";
    protected readonly string TemperatureFileKeyword = "";
    protected readonly string PropertyEnrichmentFileKeyword = "";

    protected IConfiguration configuration { get; }
    public Logger.ILogger Logger { get; }

    protected IFileWorker fileWorker;

    public string? FilesDirectory { get; set; }
    public VDMAdminDbContext Db { get; }

    protected BaseImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration _configuration, DataImportAgent.Logger.ILogger logger, IFileWorker _IFileWorker)
    {
        Db = dbContextFactory.CreateDbContext();
        Db.Database.SetCommandTimeout(TimeSpan.FromMinutes(20));
        configuration = _configuration;
        Logger = logger;
        fileWorker = _IFileWorker;

        PropertyFileKeyword = configuration["AgentSettings:PropertyFileKeyword"];
        DeviceFileKeyword = configuration["AgentSettings:DeviceFileKeyword"];
        ConsumptionUnitFileKeyword = configuration["AgentSettings:ConsumptionUnitKeyword"];
        TemperatureFileKeyword = configuration["AgentSettings:TemperatureFileKeyword"];
        deviceCategoryFileKeyword = configuration["AgentSettings:DeviceCategoryKeyword"];
        PropertyEnrichmentFileKeyword = configuration["AgentSettings:PropertyEnrichmentKeyword"];

        if (_configuration["Enviroment"] == "staging") FilesDirectory = configuration["AgentSettings:WebnimbusDirectoryStaging"];
        if (_configuration["Enviroment"] == "local") FilesDirectory = configuration["AgentSettings:WebnimbusDirectoryLocal"];
        if (_configuration["Enviroment"] == "production") FilesDirectory = configuration["AgentSettings:WebnimbusDirectoryProduction"];

        //check for missed Configurations
        if (string.IsNullOrEmpty(DeviceFileKeyword) || string.IsNullOrEmpty(DeviceFileKeyword) || string.IsNullOrEmpty(ConsumptionUnitFileKeyword))
        {
            throw new ApplicationException("Invalid Configuration set for AgentSettings files keyword . Empty values");
        }
    }

    protected void MarkFileHistoryAsSuccess(FileHistory fileHistory)
    {
        fileHistory.JobStatus = FileHistory.Status.Success;
        Db.SaveChanges();
    }

    protected void LogFileReadingError(string fileName, Exception ex)
    {
        Logger.LogError($"File reading error in file {fileName}", ex);
    }

    protected void CreateFileHistoryWithError(string fileName, string errorMessage)
    {
        var fileHistory = new FileHistory
        {
            FileName = fileName,
            JobStatus = FileHistory.Status.Failed,
            LogText = errorMessage
        };

        Db.FileHistories.Add(fileHistory);
        Db.SaveChanges();
    }

    protected FileHistory CreateFileHistory(string fileName)
    {
        return new FileHistory { FileName = fileName, JobStatus = FileHistory.Status.Success };
    }

    protected List<string?> GetNonAccessedFiles(string fileKeyword)
    {
        var accessedFiles = Db.FileHistories
            .Where(f => f.FileName.Contains(fileKeyword))
            .Select(f => f.FileName)
            .ToList();

        var nonAccessedFiles = fileWorker.GetDirectoryFiles(FilesDirectory, fileKeyword)
            .Except(accessedFiles)
            .ToList();

        if (nonAccessedFiles.Count > 0)
            Logger.LogInformation($"{nonAccessedFiles.Count} files are found to import from {typeof(T).Name}");

        return nonAccessedFiles;
    }

    /// FUNCTOIN SIGNATUES

    public abstract List<T> ReadFile(string fileName);

    public abstract void StoreItems(List<T> consumptionUnits);

    public abstract void ImportFiles();
}