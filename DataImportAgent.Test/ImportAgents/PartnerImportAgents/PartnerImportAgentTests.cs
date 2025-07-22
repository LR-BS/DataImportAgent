/*using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using DataImportAgent.PartnerAgent;
using Microsoft.EntityFrameworkCore.Internal;
using Moq;

namespace DataImportAgent.Test.ImportAgents.PartnerImportAgents
{
    public class PartnerImportAgentTests
    {
        private IConfiguration _configuration;
        private PartnerImportAgent agent;
        private VDMAdminDbContext db;
        private IDbContextFactory<VDMAdminDbContext> DbContextFactory;

        public PartnerImportAgentTests()

        {
            // Mock the IConfiguration
            DbContextOptionsBuilder<VDMAdminDbContext> builder = new DbContextOptionsBuilder<VDMAdminDbContext>();

            builder.UseInMemoryDatabase("IstaImporterJobtTestDB" + new Random().Next(1000, 9000).ToString());

            var configBuilder = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: false);
            _configuration = configBuilder.Build();
            DbContextFactory = new Mock<IDbContextFactory<VDMAdminDbContext>>().Object;
            db = new VDMAdminDbContext(builder.Options, _configuration);
            agent = new PartnerImportAgent(DbContextFactory, _configuration, null, null);
        }

        private void IntializeData()
        {
            var properties = new List<Property> {
            new Property{PartnerCode = "1",Id = Guid.NewGuid(),PropertyNumber="2205",ImportFileID=Guid.NewGuid() },
            new Property { PartnerCode = "2", Id = Guid.NewGuid(),PropertyNumber="2005",ImportFileID=Guid.NewGuid()} ,
            new Property { PartnerCode = "3", Id = Guid.NewGuid(),PropertyNumber="2003" ,ImportFileID=Guid.NewGuid()} ,
            };

            var partners = new List<Partner> {
            new Partner{PartnerCode="1",Id=Guid.NewGuid()},
            new Partner{PartnerCode="10000",Id=Guid.NewGuid()}
            };

            db.Properties.AddRange(properties);
            db.Partners.AddRange(partners);
            db.SaveChanges();
        }

        [Fact]
        public void should_not_import_duplicated_property_managers()
        {
            IntializeData();

            var partners = new List<Partner> {
            new Partner{PartnerCode="1",Id=Guid.NewGuid()}
            };

            var list = agent.EnrichPropertyManagers(partners);

            Assert.Equal(0, list.Where(a => a.PartnerCode == "1")?.Count());
        }

        [Fact]
        public void should_enrich_property_manager()
        {
            IntializeData();

            var partners = new List<Partner> {
            new Partner{PartnerCode="1",Id=Guid.NewGuid()},
            new Partner{PartnerCode="3",Id=Guid.NewGuid()}
           };

            var list = agent.EnrichPropertyManagers(partners);
            Assert.Equal(1, list.Where(a => a.PartnerCode == "3").Where(a => a.MigrationStatus == PartnerMigrationStatus.PREPARED_FOR_WP)?.Count());
        }

        
        [Fact]
        public void shoud_not_import_property_managers_without_property()
        {
            IntializeData();

            var partners = new List<Partner> {
            new Partner{PartnerCode="2909",Id=Guid.NewGuid()}
           };

            var list = agent.EnrichPropertyManagers(partners);

            Assert.Equal(0, list.Where(a => a.PartnerCode == "2")?.Count());
        }
    }
}*/