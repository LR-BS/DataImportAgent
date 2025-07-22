using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;

namespace FileImportAgent.PropertyManagerAgent;

public class SAPDbContext : DbContext
{
    public SAPDbContext()
    { }

    public SAPDbContext(DbContextOptions<SAPDbContext> builder) : base(builder)
    {
    }

    public DbSet<Partner> PropertyManagers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.EnableSensitiveDataLogging();

        if (options.IsConfigured == false)
        {
            var builder = new ConfigurationBuilder()
                  .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                  .AddJsonFile(System.AppDomain.CurrentDomain.BaseDirectory + "appsettings.json", optional: false);

            IConfiguration configuration = builder.Build();

            base.OnConfiguring(options);
            if (options.IsConfigured == false)
            {
                options.UseSqlServer(GetConnectionString(configuration));
            }
        }
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        if (configuration["Enviroment"] == "production")
            return (configuration.GetConnectionString("SAPistaProductionDB"));
        if (configuration["Enviroment"] == "local")
            return (configuration.GetConnectionString("SAPlocalDB"));
        if (configuration["Enviroment"] == "staging")
            return (configuration.GetConnectionString("SAPistaStagingDB"));

        throw new Exception("Not defined enviroment");
    }
}