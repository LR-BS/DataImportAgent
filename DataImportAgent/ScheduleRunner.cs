using DataImportAgent;
using DataImportAgent.Agents;
using DataImportAgent.Agents.ImportAgents;
using DataImportAgent.Logger;
using DataImportAgent.PartnerAgent;
using DataImportAgent.PropertyEnrichment;
using FileImportAgent.PropertyManagerAgent;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;

namespace WebnimbusDataImportAgent
{
    public class ScheduleRunner : BackgroundService
    {
        public IConfiguration Configuration { get; }
        public IDbContextFactory<VDMAdminDbContext> DbContextFactory { get; }
        public SAPDbContext SAPDbContext { get; }

        public DataImportAgent.Logger.ILogger logger;

        public ScheduleRunner(IConfiguration configuration, IDbContextFactory<VDMAdminDbContext> VDMAdminDbContextFactory, IDbContextFactory<SAPDbContext> SAPDbContextFactory)
        {
            Configuration = configuration;
            DbContextFactory = VDMAdminDbContextFactory;
            this.SAPDbContext = SAPDbContextFactory.CreateDbContext();
            logger = new Logger();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Import Agent is started! - version:" + Configuration["version"]);
            

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    new PropertyImportAgent(DbContextFactory, Configuration, logger, new FileWorker()).ImportFiles();
                    new ConsumptionUnitImportAgent(DbContextFactory, Configuration, logger, new FileWorker()).ImportFiles();
                    new DeviceImportAgent(DbContextFactory, Configuration, logger, new FileWorker()).ImportFiles();
                    new DeviceCategoryImportAgent(DbContextFactory, Configuration, logger, new FileWorker()).ImportFiles();
                    new TemperatureImportAgent(DbContextFactory, Configuration, logger, new FileWorker()).ImportFiles();
                    new PartnerImportAgent(DbContextFactory, Configuration, logger, SAPDbContext).Initialize();
                    new PropertyEnrichmentAgent(DbContextFactory, Configuration, logger, new FileWorker(), new GPSHelper()).ImportFiles();
                }
                catch (Exception ex)
                {
                    logger.LogError("MainThread exited: ", ex);
                }
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}