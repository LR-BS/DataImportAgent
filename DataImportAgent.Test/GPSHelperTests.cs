using DataImportAgent.Test.ImportAgents.consumptionUnitImportAgent;
using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataImportAgent.Test;

public class GPSHelperTests : ImportAgentTestBase
{
    /// <summary>
    /// According to VDMA-590
    /// TEST   REQUIRES INTERNET CONNECTION
    /// </summary>
    [Fact]
    public void should_return_gps_coordinates_as_per_each_property()
    {
        var service = new GPSHelper();

        Property TestProperty = new Property
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

        (double latitude, double longitude) = service.GetGeolocationData(TestProperty);

        Assert.Equal((47.940643), latitude, 1);
        Assert.Equal((16.72344), longitude, 1);
    }
}