using DataImportAgent.Agents;
using DataImportAgent.Agents.ImportAgents;
using DataImportAgent.Logger;
using DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;
using DataImportAgent.Test.MockHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DataImportAgent.Test.ImportAgents;

public class TemperatureImportAgentTests : ImportAgentTestBase
{
    [Fact]
    public async Task Should_store_temperature()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        temperatureImportAgent.ImportFiles();
        var importedTemperature = await db.Temperatures.FirstOrDefaultAsync();

        Assert.Equal(TestEntities.TestTemperature.Date, importedTemperature.Date);
        Assert.Equal(TestEntities.TestTemperature.ProvinceCode, importedTemperature.ProvinceCode);
        Assert.Equal(TestEntities.TestTemperature.Value, importedTemperature.Value);

        Assert.Equal(314, importedTemperature.Value);
    }

    [Fact]
    public void Should_deny_duplicate_temperature()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        var worker = FileWorkerMock.CreateDuplicateTemperature().Object;
        var loggerMock = new Mock<ILogger>();
        temperatureImportAgent = new TemperatureImportAgent(DbContextFactory, configuration, loggerMock.Object, worker);
        db.Temperatures.Add(TestEntities.TestTemperature);
        db.SaveChangesAsync();
        temperatureImportAgent.ImportFiles();
        string expectedLogMessage = "following state  and date are already existed";
        loggerMock.Verify(x => x.LogError(It.IsAny<string>(), It.Is<ApplicationException>(exc => exc.Message.Contains(expectedLogMessage))), Times.Once);
    }
}