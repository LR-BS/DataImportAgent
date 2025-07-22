using DataImportAgent.Agents;
using DataImportAgent.Test.MockHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using System.Security.Cryptography.X509Certificates;
using DataImportAgent.Agents.ImportAgents;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;

public class ConsumptionUnitImportAgentTests : ImportAgentTestBase
{
    [Fact]
    public async Task Should_store_consumptionunit_information()
    {
        var db = DbContextFactory.CreateDbContext();
        var testEntites = new TestEntities();

        var testProperty = testEntites.TestProperty;

        db.Properties.Add(testProperty);
        db.SaveChanges();

        consumptionUnitImportAgent.ImportFiles();
        var list = db.ConsumptionUnits.ToList();
        Assert.NotEmpty(db.ConsumptionUnits.Where(a => a.ConsumptionUnitNumber == testEntites.TestConsumptionUnit.ConsumptionUnitNumber));

        Assert.Equal(1, await db.ConsumptionUnits.CountAsync());
    }

    [Fact]
    public async Task Should_not_store_consumptionunit_with_number_9900()
    {
        var db = DbContextFactory.CreateDbContext();
        var testEntites = new TestEntities();

        var testProperty = testEntites.TestProperty;
        db.Properties.Add(testProperty);
        await db.SaveChangesAsync();

        consumptionUnitImportAgent.ImportFiles();

        Assert.Equal(0, await db.ConsumptionUnits.Where(a => a.ConsumptionUnitNumber == "9900").CountAsync());
        Assert.NotEmpty(db.ConsumptionUnits);
    }

    /*
    [Fact]
    public void Shoud_assign_tenants_to_consumptionunit()
    {
        var db = DbContextFactory.CreateDbContext();
        var testEntites = new TestEntities();

        var testProperty = testEntites.TestProperty;
        db.Properties.Add(testProperty);
        db.SaveChanges();
        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, new FileWorker())
            {
                FilesDirectory = Environment.CurrentDirectory + "/ImportAgents/consumptionUnitImportAgent/tenantsChangeTestData"
            };

        agent.ImportFiles();

        var list = db.ConsumptionUnits.Include(a => a.Tenants).ToList();

        var consumptionUnitWithTenantChange = list.FirstOrDefault(a => a.Id == Guid.Parse("F2929BF8-60B6-3A7E-859F-95422D791C35"));

        Assert.Equal(2, consumptionUnitWithTenantChange.Tenants.Count);
        Assert.Equal(new DateTime(2023, 07, 31), consumptionUnitWithTenantChange.Tenants.Where(a => a.MoveOutDate != null).FirstOrDefault().MoveOutDate);
    }
    */

    [Fact]
    public void shoud_update_existing_tenant_moveoutdate()
    {
        
        var db = DbContextFactory.CreateDbContext();
        var testEntites = new TestEntities();

        // arrange :: Enter sample property,consumptionunit and tenant to database
        var testProperty = testEntites.TestProperty;
        db.Properties.Add(testProperty);
        var testConsumptionunit = testEntites.TestConsumptionUnit;
        db.Tenants.Add(testEntites.TestTenant);
        db.SaveChanges();

        db.ChangeTracker.Clear();

        ///mock :: enter sample consumptionunit (existing) with existing tennat but with an enddate AND also a new added tenant
        var fileWorkerMock_tennants = new Mock<IFileWorker>();
        var tenant_with_enddate = testEntites.TestTenant;
        tenant_with_enddate.MoveOutDate = new DateTime(2023, 06, 10);

        fileWorkerMock_tennants.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> {
             testEntites.TestTenant2,
             tenant_with_enddate
        });
        fileWorkerMock_tennants.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> {
            testConsumptionunit
        });

        fileWorkerMock_tennants.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_2,Gerate_2,Nutzer_2,GereateMapping2" });

        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, fileWorkerMock_tennants.Object);

        /// act
        agent.ImportFiles();

        // assert
        var consumptionunits = db.ConsumptionUnits.Include(a => a.Tenants).ToList();

        Assert.Single(consumptionunits); //not having duplicate consumptionunit

        Assert.Equal(2, consumptionunits.FirstOrDefault()!.Tenants.Count); // not having duplicated tenant

        Assert.Single(consumptionunits.FirstOrDefault()!.Tenants.Where(a => a.MoveOutDate == tenant_with_enddate.MoveOutDate));  // one tenant with moveout date

        Assert.Equal(SharedKernel.Enums.TenantMigrationStatus.PREPARED_FOR_UPDATE_TO_WP,
            consumptionunits!.FirstOrDefault()!.Tenants
            .Where(a => a.MoveOutDate == tenant_with_enddate.MoveOutDate)!.FirstOrDefault()!.MigrationStatus);  // correct migrationStatus
    }

    [Fact]
    public void should_skip_VacantDwelling_is_one()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();
        ///arrange
        db.ChangeTracker.Clear();
        var testProperty = TestEntities.TestProperty;
        db.Properties.Add(testProperty);
        db.SaveChanges();
        var testConsumptionunit = TestEntities.TestConsumptionUnit;
        db.SaveChanges();

        var fileWorkerMock = new Mock<IFileWorker>();
        var empty_tanant = TestEntities.TestTenant;
        empty_tanant.VacantDwelling = true;

        /// we made a condidition where we have a vacant but without the name of Leerstand

        fileWorkerMock.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> { empty_tanant });

        fileWorkerMock.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> {
            testConsumptionunit
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);

        //act
        agent.ImportFiles();

        //assert
        Assert.Empty(db.ConsumptionUnits.SelectMany(a => a.Tenants).Where(a => a.Name == "Leerstand"));
    }

    

    /// <summary>
    /// Based on VDMA-577 requirements
    /// </summary>
    [Fact]
    public void should_skip_existing_consumptionUnit_and_same_tenants()
    {
        var db = DbContextFactory.CreateDbContext();
        //ARRANGE
        var consumptionUnit = new TestEntities().TestConsumptionUnit;
        var tenant = new TestEntities().TestTenant;
        db.Properties.RemoveRange(db.Properties.ToList());
        db.SaveChanges();
        
        db.Properties.Add(new TestEntities().TestProperty);
        db.SaveChanges();
        
        db.ConsumptionUnits.Add(consumptionUnit);
        db.Tenants.Add(tenant);
        db.SaveChanges();

        //change file
        var newConsumptionUnit = consumptionUnit;
        var newTenant = tenant;

        newConsumptionUnit.ImportFileID = Guid.NewGuid();

        var fileWorkerMock = new Mock<IFileWorker>();
        fileWorkerMock.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> { newConsumptionUnit });
        fileWorkerMock.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> { newTenant });
        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_2,Gerate_2,Nutzer_2,GereateMapping2" });

        //ACT

        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        Assert.Equal(1, db.ConsumptionUnits.Count());
        Assert.Equal(1, db.Tenants.Count());

        Assert.Equal<Tenant>(tenant, db.Tenants.FirstOrDefault());
        Assert.Equal<ConsumptionUnit>(consumptionUnit, db.ConsumptionUnits.FirstOrDefault());
    }

    /// <summary>
    /// Based on VDMA-577 requirements
    /// </summary>
    [Fact]
    public void should_skip_tenants()
    {
        var db = DbContextFactory.CreateDbContext();
        //ARRANGE
        var consumptionUnit = new TestEntities().TestConsumptionUnit;
        var tenant = new TestEntities().TestTenant;
        db.Properties.RemoveRange(db.Properties.ToList());
        db.Properties.Add(new TestEntities().TestProperty);
        db.ConsumptionUnits.Add(consumptionUnit);
        db.Tenants.Add(tenant);
        db.SaveChanges();

        //change file
        var newConsumptionUnit = consumptionUnit;
        var newTenant = tenant;

        newConsumptionUnit.ImportFileID = Guid.NewGuid();

        var fileWorkerMock = new Mock<IFileWorker>();
        fileWorkerMock.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> { newConsumptionUnit });
        fileWorkerMock.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> { newTenant });
        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_2,Gerate_2,Nutzer_2,GereateMapping2" });

        //ACT

        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        Assert.Equal(1, db.ConsumptionUnits.Count());
        Assert.Equal(1, db.Tenants.Count());

        Assert.Equal<Tenant>(tenant, db.Tenants.FirstOrDefault());
        Assert.Equal<ConsumptionUnit>(consumptionUnit, db.ConsumptionUnits.FirstOrDefault());
    }

    /// <summary>
    /// Based on VDMA-595 requirements
    /// </summary>
    [Fact]
    public void should_update_existing_consumptionUnit_when_receive_unactive_consumptionunit_and_is_SENT_TO_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.PREPARED_FOR_WP;
        var consumptionUnit = new TestEntities().TestConsumptionUnit;
        consumptionUnit.MigrationStatus = ConsumptionUnitMigrationStatus.SENT_TO_WP;
        db.Properties.Add(property);
        db.ConsumptionUnits.Add(consumptionUnit);
        var tenant = new TestEntities().TestTenant;
        db.Tenants.Add(tenant);
        db.SaveChanges();

        var updateConsumptionUnit = new TestEntities().TestConsumptionUnit;
        updateConsumptionUnit.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> {
          updateConsumptionUnit
        });
        fileWorkerMock.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> { tenant });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        var existingItem = DbContextFactory.CreateDbContext().ConsumptionUnits.SingleOrDefault();

        Assert.Equal(ConsumptionUnitMigrationStatus.PLANNED_TO_BE_DELETED, existingItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-594 requirements
    /// </summary>
    [Fact]
    public void should_delete_existing_consumptionUnit_when_receive_unactive_consumptionUnit_and_consumptionUnit_is_NOT_SENT_TO_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.SENT_TO_WP;
        var consumptionUnit = new TestEntities().TestConsumptionUnit;
        consumptionUnit.MigrationStatus = ConsumptionUnitMigrationStatus.PREPARED_FOR_WP;
        db.Properties.Add(property);
        db.ConsumptionUnits.Add(consumptionUnit);
        var tenant = new TestEntities().TestTenant;
        db.Tenants.Add(tenant);
        db.SaveChanges();

        var updateconsumptionUnit = new TestEntities().TestConsumptionUnit;
        updateconsumptionUnit.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> {
          updateconsumptionUnit
        });
        fileWorkerMock.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> { tenant });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new ConsumptionUnitImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        var existingItem = DbContextFactory.CreateDbContext().ConsumptionUnits.SingleOrDefault();
        Assert.Equal(existingItem.Id, updateconsumptionUnit.Id);
        Assert.Equal(ConsumptionUnitMigrationStatus.PLANNED_TO_BE_DELETED, existingItem.MigrationStatus);
    }
}