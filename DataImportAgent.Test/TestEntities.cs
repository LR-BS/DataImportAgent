using DataImportAgent.PropertyEnrichment;
using SharedKernel.Domain;

namespace DataImportAgent.Test;

public class TestEntities
{
    public Device TestDevice = new Device
    {
        Active = true,
        AMM_Mode = 5,
        ArticleNumber = "f14124",
        ConsumptionEndValue = 10,
        consumptionStartValue = 2,
        ConsumptionUnitId = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa6"),
        DeviceNumber = "8575161325",
        DevicePositionUUID = Guid.NewGuid(),
        DeviceSerialNumber = "Seriali94881",
        DeviceType = 6,
        EndDate = DateTime.Now.AddDays(4),
        ImportFileID = Guid.NewGuid(),
        InstallmentLocation = "JKD55155",
        Id = Guid.NewGuid(),
        ModuleSeriaNumber = "jjhgf7843",
        Scale = 333,
        Type = 6,
        StartDate = DateTime.Now
    };

    public Device TestDevicetype2 = new Device
    {
        Active = true,
        AMM_Mode = 5,
        ArticleNumber = "AJ8477441",
        ConsumptionEndValue = 10,
        consumptionStartValue = 2,
        ConsumptionUnitId = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa6"),
        DeviceNumber = "8575161325",
        DevicePositionUUID = Guid.NewGuid(),
        DeviceSerialNumber = "Seriali94881",
        DeviceType = 5,
        EndDate = DateTime.Now.AddDays(4),
        ImportFileID = Guid.NewGuid(),
        InstallmentLocation = "JKD55155",
        Id = Guid.NewGuid(),
        ModuleSeriaNumber = "jjhgf7843",
        Scale = 333,
        Type = 2,
        StartDate = DateTime.Now
    };

    public Device TestDeviceAmmMode3AndType1 = new Device
    {
        Active = true,
        AMM_Mode = 3,
        ArticleNumber = "AJ8477441",
        ConsumptionEndValue = 10,
        consumptionStartValue = 2,
        ConsumptionUnitId = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa6"),
        DeviceNumber = "8575161325",
        DevicePositionUUID = Guid.NewGuid(),
        DeviceSerialNumber = "Seriali94881",
        DeviceType = 5,
        Id = Guid.NewGuid(),
        EndDate = DateTime.Now.AddDays(4),
        ImportFileID = Guid.NewGuid(),
        InstallmentLocation = "JKD55155",
        ModuleSeriaNumber = "jjhgf7843",
        Scale = 333,
        Type = 1,
        StartDate = DateTime.Now
    };

    public Device UnActiveDevice = new Device
    {
        Active = false,
        AMM_Mode = 3,
        ArticleNumber = "AJ8477441",
        ConsumptionEndValue = 10,
        consumptionStartValue = 2,
        ConsumptionUnitId = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa4"),
        DeviceNumber = "8575161325",
        DevicePositionUUID = Guid.NewGuid(),
        DeviceSerialNumber = "Seriali948281",
        DeviceType = 5,
        Id = Guid.NewGuid(),
        EndDate = DateTime.Now.AddDays(4),
        ImportFileID = Guid.NewGuid(),
        InstallmentLocation = "JKD551155",
        ModuleSeriaNumber = "jjhgf17843",
        Scale = 333,
        Type = 1,
        StartDate = DateTime.Now
    };

    public ConsumptionUnit TestConsumptionUnit = new ConsumptionUnit
    {
        ConsumptionUnitNumber = "aFKKs",
        Active = true,
        Door = "13",
        Area = 1123,
        Block = "123",
        CommonDwelling = "22",
        ConsumptionUnitType = 12,
        Id = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa6"),
        Floor = "123",
        HouseNnumber = "123",
        ImportFileID = Guid.NewGuid(),
        IsMainMeter = false,
        PropertyId = Guid.Parse("944f48b9-413d-49d4-84fe-5b01c3edee35"),
        Staircase = "JFIFssa3",
        MigrationStatus = SharedKernel.Enums.ConsumptionUnitMigrationStatus.PREPARED_FOR_WP,
        Street = "Asjfkostreet"
    };

    public ConsumptionUnit TestConsumptionUnit9900 = new ConsumptionUnit
    {
        ConsumptionUnitNumber = "9900",
        Active = true,
        Door = "13",
        Area = 1123,
        Block = "123",
        CommonDwelling = "22",
        ConsumptionUnitType = 12,
        Id = Guid.NewGuid(),
        Floor = "123",
        HouseNnumber = "123",
        ImportFileID = Guid.NewGuid(),
        IsMainMeter = false,
        PropertyId = Guid.Parse("944f48b9-413d-49d4-84fe-5b01c3edee35"),
        Staircase = "JFIFssa3",
        Street = "Asjfkostreet"
    };

    public Property TestProperty = new Property
    {
        Active = true,
        PropertyNumber = "123131",
        Id = Guid.Parse("944f48b9-413d-49d4-84fe-5b01c3edee35"),
        Street = "Asjfkostreet",
        City = "Adg",
        ExternalCode = "SFAsf",
        Housenumber = "gkkga",
        IstaSpecialistId = "1241415",
        ImportFileID = Guid.NewGuid(),
        Kaltmiete = false,
        PartnerCode = "123",
        PostCode = "1424"
    };

    public Property TestProperty2 = new Property
    {
        Active = true,
        PropertyNumber = "123131",
        Id = Guid.NewGuid(),
        Street = "Weg",
        City = "Eisenstadt",
        ExternalCode = "SFAsf",
        Housenumber = "17",
        IstaSpecialistId = "1241415",
        ImportFileID = Guid.NewGuid(),
        Kaltmiete = false,
        PartnerCode = "123",
        PostCode = "7100"
    };

    public Property TestProperty3 = new Property
    {
        Active = true,
        PropertyNumber = "123132",
        Id = Guid.Parse("944f48b9-413d-49d5-84fe-5b01c3edee35"),
        Street = "1a",
        City = "615",
        ExternalCode = "aff`",
        Housenumber = "15551",
        IstaSpecialistId = "661",
        ImportFileID = Guid.NewGuid(),
        Kaltmiete = false,
        PartnerCode = "1",
        PostCode = "1"
    };

    public DeviceCategory TestCategory = new DeviceCategory
    {
        Conversion = "asda",
        ArticleNumber = "f14124",
        DeviceType = 6,
        EnergyCarrierId = 123,
        ExtrapolationCategory = "1231",
        Id = Guid.NewGuid(),
        ImportFileID = Guid.NewGuid(),
        SapDescription = "123123",
        Title = "title1",
        Unit = "13123"
    };

    public Temperature TestTemperature = new Temperature
    {
        Date = DateTime.ParseExact("2022/02/01", "yyyy/mm/dd", null),
        ImportFileID = Guid.NewGuid(),
        ProvinceCode = 21441,
        Value = 314
    };

    public PropertyEnrichmentInformation TestPropertyEnrichment = new()
    {
        PropertyNumber = "1",
        ContractNumber = "2",
        StartDate = new DateTime(2022, 01, 01)
    };

    public Tenant TestTenant = new()
    {
        ConsumptionUnitId = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa6"),
        Id = Guid.NewGuid(),
        MoveInDate = new DateTime(2022, 01, 01),
        MoveOutDate = null,
        Name = "Sample Tenant",
        ExternalTenantId = new Random().Next(10000, 99000).ToString(),
        VacantDwelling = false
    };

    public Tenant TestTenant2 = new()
    {
        ConsumptionUnitId = Guid.Parse("15cb8875-b120-4185-8af0-d042d2278aa6"),
        Id = Guid.NewGuid(),
        MoveInDate = new DateTime(2023, 01, 01),
        ExternalTenantId = new Random().Next(10000, 99000).ToString(),
        MoveOutDate = null,
        Name = "Sample Tenant 2",
        VacantDwelling = false
    };
}