using DataImportAgent.PropertyEnrichment;
using SharedKernel.Domain;
using WebnimbusDataImportAgent;
using WebnimbusDataImportAgent.Mappers;

namespace FileImportAgent.Test
{
    public class FileWorkerTests
    {
        public string prePath = Environment.CurrentDirectory + "/TestData/";

        public string TestDeviceFilePath;
        public string TestCategoryFilePath;
        public string TestPropertyFilePath;
        public string TestConsumptionFilePath;
        private string TestTemperatureFilePath;
        private string TestPropertyEnrichmentFilePath;

        private IFileWorker worker = null;

        public FileWorkerTests()
        {
            TestDeviceFilePath = prePath + "Gerate_4347316399_202301241123.csv";
            TestCategoryFilePath = prePath + "GereateMapping.csv";
            TestPropertyFilePath = prePath + "Liegenschaft_4347316399_202301241123.csv";
            TestConsumptionFilePath = prePath + "Nutzer_test_data.csv";
            TestPropertyEnrichmentFilePath = prePath + "PropertyEnrichment.csv";
            TestTemperatureFilePath = prePath + "Messdaten_22441.csv";

            worker = new FileWorker();
        }

        [Fact]
        public void Should_List_Files_in_the_test_directory_correctly()
        {
            List<string> DeviceFileList = worker.GetDirectoryFiles(prePath, "Gerate_");
            List<string> CategoryFileList = worker.GetDirectoryFiles(prePath, "GereateMapping");
            List<string> PropertyFileList = worker.GetDirectoryFiles(prePath, "Liegenschaft");
            List<string> ConsumptionFileList = worker.GetDirectoryFiles(prePath, "Nutzer");
            List<string> PropertyEnrichmentFileList = worker.GetDirectoryFiles(prePath, "PropertyEnrichment");

            Assert.Equal(new List<string> { TestDeviceFilePath }, DeviceFileList);
            Assert.Equal(new List<string> { TestCategoryFilePath }, CategoryFileList);
            Assert.Equal(new List<string> { TestPropertyFilePath }, PropertyFileList);
            Assert.Equal(new List<string> { TestConsumptionFilePath }, ConsumptionFileList);
            Assert.Equal(new List<string> { TestPropertyEnrichmentFilePath }, PropertyEnrichmentFileList);
        }

        [Fact]
        public void Should_Read_Webnimbus_File_DeviceCategory_Correctly()
        {
            List<DeviceCategory> list = worker.ReadFile<DeviceCategory, DeviceCategoryMapper>(TestCategoryFilePath);

            Assert.Equal(29, list.Count);

            //checksome for all article numbers
            Assert.Equal(478536, list.Sum(T => Convert.ToInt32(T.ArticleNumber)));

            //TODO add random validation for all record fields
        }

        [Fact]
        public void Should_Read_Webnimbus_File_Devices_Correctly()
        {
            List<Device> list = worker.ReadFile<Device, DeviceMapper>(TestDeviceFilePath);

            Assert.Equal(130, list.Count);

            //checksome for all article numbers
            Assert.Equal(2264718, list.Sum(T => Convert.ToInt32(T.ArticleNumber)));

            //TODO add random validation for all record fields
        }

        [Fact]
        public void Should_Read_Webnimbus_File_Properties_Correctly()
        {
            List<Property> list = worker.ReadFile<Property, PropertyMapper>(TestPropertyFilePath);

            Assert.Equal(1, list.Count);

            //checksome for all article numbers
            Assert.Equal(4731, list.Sum(T => Convert.ToInt32(T.PostCode)));

            //TODO add random validation for all record fields
        }

        [Fact]
        public void Should_Read_Webnimbus_File_ConsumptionUnits_Correctly()
        {
            List<ConsumptionUnit> list = worker.ReadFile<ConsumptionUnit, ConsumptionUnitMapper>(TestConsumptionFilePath);

            Assert.Equal(30, list.Count);

            Assert.Equal(1933, list.Sum(T => Convert.ToInt32(T.HouseNnumber)));
        }

        [Fact]
        public void Should_Read_Property_EnrichmentFiles_Correctly()
        {
            List<PropertyEnrichmentInformation> list = worker.ReadFile<PropertyEnrichmentInformation, PropertyEnrichmentMapper>(TestPropertyEnrichmentFilePath);

            Assert.Equal(12, list.Count);
            Assert.Equal("431234013", list.LastOrDefault().PropertyNumber);
        }

        [Fact]
        public void Should_Read_Temperature_File_Correctly()
        {
            List<Temperature> list = worker.ReadFile<Temperature, TemperatureMapper>(TestTemperatureFilePath);

            Assert.Equal(7, list.Count);

            Assert.Equal(236, list.Sum(T => Convert.ToInt32(T.Value)));
            Assert.True(list.All(item => item.Date != null));
            Assert.True(list.All(item => item.ProvinceCode != null));
        }
    }
}