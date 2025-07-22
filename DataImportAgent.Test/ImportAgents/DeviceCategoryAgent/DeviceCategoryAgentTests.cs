using DataImportAgent.Agents;
using DataImportAgent.Logger;
using DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;
using DataImportAgent.Test.MockHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DataImportAgent.Test.ImportAgents;

public class DeviceCategoryAgentTests : ImportAgentTestBase
{
    [Fact]
    public async Task Should_store_devicecategory_information()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.Add(TestEntities.TestProperty);
        db.ConsumptionUnits.Add(TestEntities.TestConsumptionUnit);
        db.Devices.Add(TestEntities.TestDevice);

        db.SaveChanges();

        deviceCategoryImportAgent.ImportFiles();

        var savedItem = await db.DeviceCategories.FirstOrDefaultAsync();
        Assert.Equal(TestEntities.TestCategory.ArticleNumber, savedItem.ArticleNumber);
        Assert.Equal(TestEntities.TestCategory.DeviceType, savedItem.DeviceType);

        Assert.Equal(1, await db.DeviceCategories.CountAsync());
    }
}