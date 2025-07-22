using DataImportAgent.Agents;
using DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;
using DataImportAgent.Test.MockHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Domain;
using SharedKernel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataImportAgent.Agents.ImportAgents;
using SharedKernel.Data;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Test.ImportAgents;

public class PropertyImportAgentTests : ImportAgentTestBase
{
    /// <summary>
    /// TEST   REQUIRES INTERNET CONNECTION
    /// </summary>
    [Fact]
    public void should_update_property_infomration_by_enrichment_agent()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.RemoveRange(db.Properties.ToList());

        var property = TestEntities.TestProperty;

        property.PropertyNumber = TestEntities.TestPropertyEnrichment.PropertyNumber;

        db.Properties.Add(property);
        db.Properties.Add(TestEntities.TestProperty2);
        db.SaveChanges();

        propertyEnrichmentAgent.ImportFiles();

        Assert.Single(db.Properties.Where(a => a.PropertyNumber == TestEntities.TestPropertyEnrichment.PropertyNumber));
        Assert.Single(db.Properties.Where(a => a.ContractNumber == TestEntities.TestPropertyEnrichment.ContractNumber));
        Assert.Single(db.Properties.Where(a => a.StartDate == TestEntities.TestPropertyEnrichment.StartDate));
    }

    [Fact]
    public void should_deny_property_enrichment_when_enrichment_property_number_is_wrong()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.RemoveRange(db.Properties.ToList());
        var property = TestEntities.TestProperty;

        property.PropertyNumber = "1000"; //wrong propertynumber

        db.Properties.Add(property);
        db.Properties.Add(TestEntities.TestProperty2);
        db.SaveChanges();

        propertyEnrichmentAgent.ImportFiles();

        Assert.Single(db.FileHistories.Where(a => a.JobStatus == FileHistory.Status.Failed));
        Assert.Single(db.FileHistories.Where(a => a.LogText.Contains("is not found")));
    }

    [Fact]
    public async Task Should_store_property_information()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        propertyImportAgent.ImportFiles();

        var item = await db.Properties.FirstOrDefaultAsync();

        Assert.Equal(TestEntities.TestProperty.PropertyNumber, item?.PropertyNumber);

        Assert.Equal(1, await db.Properties.CountAsync());
    }

    /// <summary>
    /// Based on VDMA-576 requirements
    /// </summary>

    [Fact]
    public void should_update_existing_property_when_property_is_sent_to_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        // Arrange
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.SENT_TO_WP;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;

        updateProperty.Street += "updated";
        updateProperty.ExternalCode += "updated";
        updateProperty.PostCode += "updated";
        updateProperty.City += "updated";
        updateProperty.Housenumber += "updated";
        updateProperty.GPSLatitude += 1.0;
        updateProperty.GPSLongitude += 1.0;
        updateProperty.ContractNumber += "updated";
        updateProperty.Kaltmiete = false;
        updateProperty.IstaSpecialistId += "updated";
        updateProperty.PartnerCode += "updated";
        updateProperty.StartDate = DateTime.Now;
        updateProperty.ImportFileID = Guid.NewGuid();
        updateProperty.MigrationStatus = PropertyMigrationStatus.NOT_SET;

        /// Act

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> {
            updateProperty,
            new TestEntities().TestProperty2
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "newPropertyFile.csv" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        // Assert
        Assert.Equal(2, db.Properties.Count());

        db.Dispose();
        db = DbContextFactory.CreateDbContext();
        var savedItem = db.Properties.Where(a => a.PropertyNumber == updateProperty.PropertyNumber).FirstOrDefault()!;
        Assert.Equal(updateProperty.PropertyNumber, savedItem.PropertyNumber);
        Assert.Equal(updateProperty.ExternalCode, savedItem.ExternalCode);
        Assert.Equal(updateProperty.PostCode, savedItem.PostCode);
        Assert.Equal(updateProperty.City, savedItem.City);
        Assert.Equal(updateProperty.Street, savedItem.Street);
        Assert.Equal(updateProperty.Housenumber, savedItem.Housenumber);
        Assert.Equal(updateProperty.Kaltmiete, savedItem.Kaltmiete);
        Assert.Equal(updateProperty.PartnerCode, savedItem.PartnerCode);
        //Assert.Contains("newPropertyFile.csv", savedItem.ImportedFile.FileName);
        Assert.Equal(PropertyMigrationStatus.EDITED, savedItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-576 requirements
    /// </summary>
    [Fact]
    public void should_update_existing_property_when_property_is_NOT_sent_to_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        // Arrange
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.PREPARED_FOR_WP;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;

        updateProperty.Street += "updated";
        updateProperty.ExternalCode += "updated";
        updateProperty.PostCode += "updated";
        updateProperty.City += "updated";
        updateProperty.Housenumber += "updated";
        updateProperty.GPSLatitude += 1.0;
        updateProperty.GPSLongitude += 1.0;
        updateProperty.ContractNumber += "updated";
        updateProperty.Kaltmiete = false;
        updateProperty.IstaSpecialistId += "updated";
        updateProperty.PartnerCode += "updated";
        updateProperty.StartDate = DateTime.Now;
        updateProperty.ImportFileID = Guid.NewGuid();
        updateProperty.MigrationStatus = PropertyMigrationStatus.NOT_SET;

        /// Act

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> {
            updateProperty,
            new TestEntities().TestProperty2
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "newPropertyFile.csv" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        // Assert
        Assert.Equal(2, db.Properties.Count());
        db.Dispose();
        db = DbContextFactory.CreateDbContext();
        
        var savedItem = db.Properties.Where(a => a.PropertyNumber == updateProperty.PropertyNumber).FirstOrDefault()!;
        Assert.Equal(updateProperty.PropertyNumber, savedItem.PropertyNumber);
        Assert.Equal(updateProperty.ExternalCode, savedItem.ExternalCode);
        Assert.Equal(updateProperty.PostCode, savedItem.PostCode);
        Assert.Equal(updateProperty.City, savedItem.City);
        Assert.Equal(updateProperty.Street, savedItem.Street);
        Assert.Equal(updateProperty.Housenumber, savedItem.Housenumber);
        Assert.Equal(updateProperty.Kaltmiete, savedItem.Kaltmiete);
        Assert.Equal(updateProperty.PartnerCode, savedItem.PartnerCode);
//        Assert.Contains("newPropertyFile.csv", savedItem.ImportedFile.FileName);
        Assert.NotEqual(updateProperty.MigrationStatus, savedItem.MigrationStatus); //make sure migration status is not updated
    }

    /// <summary>
    /// Based on VDMA-576 requirements
    /// </summary>
    [Fact]
    public void should_skip_existing_property()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.PREPARED_FOR_WP;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;
        updateProperty.MigrationStatus = PropertyMigrationStatus.NOT_SET;

        /// Act

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> {
          updateProperty
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        var existingItem = db.Properties.SingleOrDefault();

        Assert.Equal<Property>(property, existingItem);
        Assert.Equal(property.MigrationStatus, existingItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-594 requirements
    /// </summary>
    [Fact]
    public void should_update_existing_property_when_receive_unactive_property_and_property_is_SENT_TO_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.SENT_TO_WP;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;
        updateProperty.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns([updateProperty]);

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        var existingItem = DbContextFactory.CreateDbContext().Properties.SingleOrDefault();

        Assert.Equal(PropertyMigrationStatus.REQUESTED_TO_BE_DELETED, existingItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-594 requirements
    /// </summary>
    [Fact]
    public void should_update_existing_property_when_receive_unactive_property_and_property_is_ASSIGNED_TO_PARTNER()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.ASSIGNED_TO_PARTNER;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;
        updateProperty.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> {
          updateProperty
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        
        var existingItem = DbContextFactory.CreateDbContext().Properties.SingleOrDefault();

        Assert.Equal(PropertyMigrationStatus.REQUESTED_TO_BE_DELETED, existingItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-594 requirements
    /// </summary>
    [Fact]
    public void should_delete_existing_property_when_receive_unactive_property_and_property_is_NOT_SENT_TO_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.MigrationStatus = PropertyMigrationStatus.PREPARED_FOR_WP;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;
        updateProperty.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> {
          updateProperty
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        db.Dispose();
        db = DbContextFactory.CreateDbContext();
        var existingItem = db.Properties.SingleOrDefault();
        Assert.Equal(updateProperty.Id, existingItem.Id);
        Assert.Equal(PropertyMigrationStatus.REQUESTED_TO_BE_DELETED, existingItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-665 requirements - we shouldn't update existing prperties when ManuallyUpdated = true
    /// </summary>

    [Fact]
    public void should_skip_properties_update_when_manually_updated_in_wp()
    {
        var db = DbContextFactory.CreateDbContext();
        // Arrange
        db.Properties.RemoveRange(db.Properties.ToList());
        var property = new TestEntities().TestProperty;
        property.ManuallyUpdated = true;
        property.MigrationStatus = PropertyMigrationStatus.SENT_TO_WP;
        db.Properties.Add(property);
        db.SaveChanges();

        var updateProperty = new TestEntities().TestProperty;

        updateProperty.Street += "updated";
        updateProperty.ImportFileID = Guid.NewGuid();
        updateProperty.MigrationStatus = PropertyMigrationStatus.NOT_SET;

        /// Act

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> {
            updateProperty,
            new TestEntities().TestProperty2
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "newPropertyFile.csv" });

        var agent = new PropertyImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        // Assert
        Assert.Equal(2, db.Properties.Count());

        var savedItem = db.Properties.Where(a => a.PropertyNumber == updateProperty.PropertyNumber).FirstOrDefault()!;

        Assert.NotEqual(updateProperty.Street, savedItem.Street);
    }
}