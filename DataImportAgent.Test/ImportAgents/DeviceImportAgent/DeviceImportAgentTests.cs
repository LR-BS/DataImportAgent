using DataImportAgent.Agents;
using DataImportAgent.Agents.ImportAgents;
using DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;
using DataImportAgent.Test.MockHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Domain;
using SharedKernel.Enums;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Test.ImportAgents;

public class DeviceImportAgentTests : ImportAgentTestBase
{
    [Fact]
    public async Task Should_not_store_device_with_type_2()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.Add(TestEntities.TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(TestEntities.TestConsumptionUnit);
        db.SaveChanges();

        deviceImportAgent.ImportFiles();
        Assert.Equal(0, await db.Devices.Where(a => a.Type == 2).CountAsync());
    }

    [Fact]
    public async Task Should_not_store_device_with_type_1_and_ammmode_3()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.Add(TestEntities.TestProperty);
        db.SaveChanges();

        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(TestEntities.TestConsumptionUnit);
        db.SaveChanges();

        deviceImportAgent.ImportFiles();
        Assert.Equal(0, await db.Devices.Where(a => a.Type == 1).Where(b => b.AMM_Mode == 3).CountAsync());
    }

    [Fact]
    public async Task Should_not_store_deleted_device()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.Add(TestEntities.TestProperty);
        db.SaveChanges();

        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(TestEntities.TestConsumptionUnit);
        db.SaveChanges();

        deviceImportAgent.ImportFiles();

        Assert.Equal(0, await db.Devices.Where(a => a.Active == false).CountAsync());
    }

    [Fact]
    public async Task Should_store_device_information()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();

        db.Properties.Add(TestEntities.TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(TestEntities.TestConsumptionUnit);
        db.SaveChanges();

        deviceImportAgent.ImportFiles();

        Assert.Equal(TestEntities.TestDevice.DeviceNumber, db.Devices.FirstOrDefault().DeviceNumber);
        Assert.Equal(TestEntities.TestDevice.DeviceSerialNumber, db.Devices.FirstOrDefault().DeviceSerialNumber);

        Assert.Equal(1, await db.Devices.CountAsync());
    }
    
    [Fact]
    public async Task Should_not_store_device_information_withoutCU()
    {
        var db = DbContextFactory.CreateDbContext();
        var TestEntities = new TestEntities();
        
        db.Properties.Add(TestEntities.TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();
        
        deviceImportAgent.ImportFiles();


        Assert.Equal(0, await db.Devices.CountAsync());
    }
    
    

    /// <summary>
    /// Based on VDMA-598
    /// </summary>
    [Fact]
    public void should_update_devices_when_status_old_device_is_NOT_sent_to_wp()
    {
        var db = DbContextFactory.CreateDbContext();
        //arrange

        db.Properties.Add(new TestEntities().TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(new TestEntities().TestConsumptionUnit);
        db.SaveChanges();

        var oldDevice = new TestEntities().TestDevice;
        oldDevice.MigrationStatus = DeviceMigrationStatus.PREPARED_FOR_WP;
        oldDevice.EndDate = null;
        db.Devices.Add(oldDevice);
        db.SaveChanges();

        var updatingDevice = new TestEntities().TestDevice;
        updatingDevice.EndDate = DateTime.Now.AddDays(3);
        updatingDevice.DevicePositionUUID = oldDevice.DevicePositionUUID;
        updatingDevice.Id = oldDevice.Id;

        var newposition_new_device = new TestEntities().TestDevice;

        var newDevice = new TestEntities().TestDevice;
        newDevice.DevicePositionUUID = oldDevice.DevicePositionUUID;
        newDevice.EndDate = null;

        var newDeviceList = new List<Device>()
        {
            updatingDevice,
           newDevice,
           newposition_new_device,
         };
        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Device, DeviceMapper>(It.IsAny<string>())).Returns(newDeviceList);

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_2,Gerate_2,Nutzer_2,GereateMapping2" });

        //act
        new DeviceImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object).ImportFiles();

        //assest
        Assert.Equal(3, db.Devices.Count());
        Assert.Equal(oldDevice.Id, db.Devices.Where(a => a.EndDate != null)!.FirstOrDefault()!.Id);
        Assert.Equal(DeviceMigrationStatus.PREPARED_FOR_UPDTE_WP, db.Devices.Where(a => a.Id == newDevice.Id)!.FirstOrDefault()!.MigrationStatus);
        //Assert.Equal(DeviceMigrationStatus.REPLACED_AND_NOT_SENT, db.Devices.Where(a => a.Id == oldDevice.Id)!.FirstOrDefault()!.MigrationStatus);
        Assert.Equal(DeviceMigrationStatus.NOT_SET, db.Devices.Where(a => a.Id == newposition_new_device.Id)!.FirstOrDefault()!.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-598
    /// </summary>
    [Fact]
    public void should_update_devices_when_status_old_device_IS_SENT_to_wp()
    {  //arrange
        var db = DbContextFactory.CreateDbContext();
        db.Properties.Add(new TestEntities().TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(new TestEntities().TestConsumptionUnit);
        db.SaveChanges();

        var oldDevice = new TestEntities().TestDevice;
        oldDevice.MigrationStatus = DeviceMigrationStatus.SENT_TO_WP;
        oldDevice.EndDate = null;
        db.Devices.Add(oldDevice);
        db.SaveChanges();

        var updatingDevice = new TestEntities().TestDevice;
        updatingDevice.EndDate = DateTime.Now.AddDays(3);
        updatingDevice.DevicePositionUUID = oldDevice.DevicePositionUUID;
        updatingDevice.Id = oldDevice.Id;

        var newDevice = new TestEntities().TestDevice;
        newDevice.DevicePositionUUID = oldDevice.DevicePositionUUID;
        newDevice.EndDate = null;
        var newDeviceList = new List<Device>()
        {
            updatingDevice,
           newDevice
         };
        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Device, DeviceMapper>(It.IsAny<string>())).Returns(newDeviceList);

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_2,Gerate_2,Nutzer_2,GereateMapping2" });

        //act
        new DeviceImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object).ImportFiles();

        //assest
        Assert.Equal(2, db.Devices.Count());
        Assert.Equal(oldDevice.Id, db.Devices.Where(a => a.EndDate != null)!.FirstOrDefault()!.Id);
        Assert.Equal(DeviceMigrationStatus.PREPARED_FOR_UPDTE_WP, db.Devices.Where(a => a.Id == newDevice.Id)!.FirstOrDefault()!.MigrationStatus);
        //Assert.Equal(DeviceMigrationStatus.REPLACED_AND_NOT_SENT, db.Devices.Where(a => a.Id == updatingDevice.Id)!.FirstOrDefault()!.MigrationStatus);
    }

    [Fact]
    public void should_skip_devices_with_the_same_properties()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.Add(new TestEntities().TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(new TestEntities().TestConsumptionUnit);
        db.SaveChanges();

        var oldDevice = new TestEntities().TestDevice;
        oldDevice.MigrationStatus = DeviceMigrationStatus.SENT_TO_WP;
        oldDevice.EndDate = null;
        db.Devices.Add(oldDevice);
        db.SaveChanges();

        var new_same_Device = new TestEntities().TestDevice;
        new_same_Device.Id = oldDevice.Id;
        new_same_Device.DevicePositionUUID = oldDevice.DevicePositionUUID;
        new_same_Device.StartDate = oldDevice.StartDate;
        new_same_Device.MigrationStatus = DeviceMigrationStatus.NOT_SET;
        new_same_Device.EndDate = null;

        new_same_Device.ImportFileID = Guid.NewGuid();

        var fileWorkerMock = new Mock<IFileWorker>();
        fileWorkerMock.Setup(t => t.ReadFile<Device, DeviceMapper>(It.IsAny<string>())).Returns(new List<Device> { new_same_Device });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "testFileName.csv" });

        //act
        new DeviceImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object).ImportFiles();

        //assest
        Assert.Equal(1, db.Devices.Count());
        Assert.Equal(oldDevice.ImportFileID, db.Devices.FirstOrDefault()!.ImportFileID);
        Assert.Equal(DeviceMigrationStatus.SENT_TO_WP, db.Devices.FirstOrDefault()!.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-697 requirements
    /// </summary>
    [Fact]
    public void should_update_existing_device_when_receive_unactive_device_and_is_SENT_TO_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.Add(new TestEntities().TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(new TestEntities().TestConsumptionUnit);
        db.SaveChanges();
        var device = new TestEntities().TestDevice;
        device.MigrationStatus = DeviceMigrationStatus.SENT_TO_WP;
        db.Devices.Add(device);
        db.SaveChanges();

        var updateDevice = new TestEntities().TestDevice;
        updateDevice.Id = device.Id;
        updateDevice.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Device, DeviceMapper>(It.IsAny<string>())).Returns(new List<Device> {
          updateDevice
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new DeviceImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        var existingItem = db.Devices.SingleOrDefault()!;

        //Assert.Equal(DeviceMigrationStatus.PLANNED_TO_BE_DELETED, existingItem.MigrationStatus);
    }

    /// <summary>
    /// Based on VDMA-697 requirements
    /// </summary>
    [Fact]
    public void should_update_existing_device_when_receive_unactive_device_and_is_NOT_SENT_TO_WP()
    {
        var db = DbContextFactory.CreateDbContext();
        db.Properties.Add(new TestEntities().TestProperty);
        db.SaveChanges();
        db.ConsumptionUnits.RemoveRange(db.ConsumptionUnits.ToList());
        db.SaveChanges();

        db.ConsumptionUnits.Add(new TestEntities().TestConsumptionUnit);
        db.SaveChanges();
        var device = new TestEntities().TestDevice;
        device.MigrationStatus = DeviceMigrationStatus.NOT_SET;
        db.Devices.Add(device);
        db.SaveChanges();

        var updateDevice = new TestEntities().TestDevice;
        updateDevice.Id = device.Id;
        updateDevice.Active = false;

        var fileWorkerMock = new Mock<IFileWorker>();

        fileWorkerMock.Setup(t => t.ReadFile<Device, DeviceMapper>(It.IsAny<string>())).Returns(new List<Device> {
          updateDevice
        });

        fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

        var agent = new DeviceImportAgent(DbContextFactory, configuration, logger, fileWorkerMock.Object);
        agent.ImportFiles();

        var existingItem = db.Devices.SingleOrDefault()!;

        //Assert.Equal(existingItem.MigrationStatus, DeviceMigrationStatus.READY_TO_DELETE_FROM_DB);
    }
}