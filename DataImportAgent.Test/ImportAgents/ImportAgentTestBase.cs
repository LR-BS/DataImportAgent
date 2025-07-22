using DataImportAgent.Agents;
using DataImportAgent.Agents.ImportAgents;
using DataImportAgent.PropertyEnrichment;
using DataImportAgent.Test.MockHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SharedKernel.Data;
using WebnimbusDataImportAgent;
using Xunit.Abstractions;

namespace DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;

public abstract class ImportAgentTestBase
{
    protected readonly IFileWorker worker;

    protected DeviceImportAgent deviceImportAgent;
    protected DeviceCategoryImportAgent deviceCategoryImportAgent;
    protected TemperatureImportAgent temperatureImportAgent;
    protected PropertyImportAgent propertyImportAgent;

    protected ConsumptionUnitImportAgent consumptionUnitImportAgent;
    protected IConfiguration configuration;
    protected DataImportAgent.Logger.ILogger logger;
    protected PropertyEnrichmentAgent propertyEnrichmentAgent;
    protected IDbContextFactory<VDMAdminDbContext> DbContextFactory;
    protected Mock<IConfiguration> mockConfig;
    SqliteConnection _connection = new SqliteConnection("DataSource=:memory:; Foreign Keys=False");

    public ImportAgentTestBase()
    {
        _connection.Open();
        
        worker = FileWorkerMock.CreateFileWorkerMock().Object;
        /*
        DbContextOptionsBuilder<VDMAdminDbContext> builder = new DbContextOptionsBuilder<VDMAdminDbContext>();
        
        builder.EnableSensitiveDataLogging();
        builder.UseInMemoryDatabase("IstaImporterJobtTestDB" + new Random().Next(100000, 900000).ToString());
        */
        
        var configBuilder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false);
        logger = new LoggerMock();
        var MockFactory = new Mock<IDbContextFactory<VDMAdminDbContext>>();
        mockConfig = new Mock<IConfiguration>();
        // Setup mock configuration values
        mockConfig.SetupGet(m => m[It.Is<string>(s => s == "Enviroment")]).Returns("test");
        mockConfig.SetupGet(m => m[It.Is<string>(s => s == "ApiTokenStaging")]).Returns("test_token");
        mockConfig.SetupGet(m => m[It.Is<string>(s => s == "ApiStagingUrl")]).Returns("test_url");
        mockConfig.SetupGet(m => m[It.Is<string>(s => s == "ApiStagingUrl")]).Returns("test_url");
        var options = new DbContextOptionsBuilder<VDMAdminDbContext>()
            .UseSqlite(_connection) // Use SQLite in-memory database
            .Options;
        
        

        var dbContext = new VDMAdminDbContext(options, mockConfig.Object);
        dbContext.Database.EnsureCreated();
        //dbContext.Database.Migrate();
        MockFactory.Setup(f => f.CreateDbContext()).Returns(() => new VDMAdminDbContext(options, mockConfig.Object));

        
        DbContextFactory = MockFactory.Object;
        Assert.NotNull(DbContextFactory.CreateDbContext());
        
        configuration = configBuilder.Build();
        ClearDatabase(DbContextFactory.CreateDbContext());
        //db = new VDMAdminDbContext(builder.Options, configuration);
        deviceCategoryImportAgent = new DeviceCategoryImportAgent(DbContextFactory, configuration, logger, worker);
        deviceImportAgent = new DeviceImportAgent(DbContextFactory, configuration, logger, worker);
        temperatureImportAgent = new TemperatureImportAgent(DbContextFactory, configuration, logger, worker);
        propertyEnrichmentAgent = new PropertyEnrichmentAgent(DbContextFactory, configuration, logger, worker, new GPSHelper());
        consumptionUnitImportAgent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, worker);
        propertyImportAgent = new PropertyImportAgent(DbContextFactory, configuration, logger, worker);
    }
    
    
    private void ClearDatabase(VDMAdminDbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw("DELETE FROM Devices");
        dbContext.Database.ExecuteSqlRaw("DELETE FROM Properties");
        dbContext.Database.ExecuteSqlRaw("DELETE FROM ConsumptionUnits");
        // Add more tables as needed
        dbContext.SaveChanges();
    }
    
    

}