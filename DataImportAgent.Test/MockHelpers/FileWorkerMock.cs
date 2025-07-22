using DataImportAgent.PropertyEnrichment;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace DataImportAgent.Test.MockHelpers
{
    public static class FileWorkerMock
    {
        public static Mock<IFileWorker> CreateFileWorkerMock()
        {
            var TestEntities = new TestEntities();

            var fileWorkerMock = new Mock<IFileWorker>();

            fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

            fileWorkerMock.Setup(t => t.ReadFile<Device, DeviceMapper>(It.IsAny<string>())).Returns(new List<Device> { TestEntities.TestDevice, TestEntities.TestDevicetype2, TestEntities.TestDeviceAmmMode3AndType1, TestEntities.UnActiveDevice });
            fileWorkerMock.Setup(t => t.ReadFile<Property, PropertyMapper>(It.IsAny<string>())).Returns(new List<Property> { TestEntities.TestProperty });
            fileWorkerMock.Setup(t => t.ReadFile<DeviceCategory, DeviceCategoryMapper>(It.IsAny<string>())).Returns(new List<DeviceCategory> { TestEntities.TestCategory });
            fileWorkerMock.Setup(t => t.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(It.IsAny<string>())).Returns(new List<ConsumptionUnit> { TestEntities.TestConsumptionUnit, TestEntities.TestConsumptionUnit9900 });
            fileWorkerMock.Setup(t => t.ReadFile<Temperature, TemperatureMapper>(It.IsAny<string>())).Returns(new List<Temperature> { TestEntities.TestTemperature });
            fileWorkerMock.Setup(t => t.ReadFile<PropertyEnrichmentInformation, PropertyEnrichmentMapper>(It.IsAny<string>())).Returns(new List<PropertyEnrichmentInformation> { TestEntities.TestPropertyEnrichment });
            fileWorkerMock.Setup(t => t.ReadFile<Tenant, TenantMapper>(It.IsAny<string>())).Returns(new List<Tenant> { new TestEntities().TestTenant });

            // Set up other mock behavior as needed

            return fileWorkerMock;
        }

        public static Mock<IFileWorker> CreateDuplicateTemperature()
        {
            var TestEntities = new TestEntities();

            var fileWorkerMock = new Mock<IFileWorker>();

            fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

            fileWorkerMock.Setup(t => t.GetDirectoryFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new List<string> { "Liegenschaft_,Gerate_,Nutzer_,GereateMapping" });

            fileWorkerMock.Setup(t => t.ReadFile<Temperature, TemperatureMapper>(It.IsAny<string>())).Returns(new List<Temperature> { TestEntities.TestTemperature });
            fileWorkerMock.Setup(t => t.ReadFile<Temperature, TemperatureMapper>(It.IsAny<string>())).Returns(new List<Temperature> { TestEntities.TestTemperature });

            return fileWorkerMock;
        }
    }
}