using DataImportAgent.Logger;
using FileImportAgent.PropertyManagerAgent;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;
using SharedKernel.Enums;
using ILogger = DataImportAgent.Logger.ILogger;

namespace DataImportAgent.PartnerAgent;

public class PartnerImportAgent
{
    private IConfiguration configuration;
    private readonly ILogger logger;
    private readonly VDMAdminDbContext db;
    private readonly SAPDbContext SAPDbContext;

    public PartnerImportAgent(IDbContextFactory<VDMAdminDbContext> dbContextFactory, IConfiguration configuration, ILogger logger, SAPDbContext SAPDbContext)
    {
        this.configuration = configuration;
        this.logger = logger;
        db = dbContextFactory.CreateDbContext();
        this.SAPDbContext = SAPDbContext;
    }

    public List<Partner> LoadPropertyManagers()
    {
        return SAPDbContext.PropertyManagers.FromSqlRaw("select newid() as Id,NAME1 as name ,STRAS as street,PSTLZ as postalcode,ORT01 as city,cast(KUNNR as nvarchar(20)) as PartnerCode ,'' as token,10 as migrationStatus,'' as SerialLetterFile  from  KNA1 ").AsNoTracking().ToList();
    }

    public List<Partner> EnrichPropertyManagers(List<Partner> PropertyManagers)
    {
        var properties = db.Properties.Where(a => a.PartnerCode != null).AsNoTracking().ToList();

        var ExistedPropertyManagers = db.Partners.Select(a => a.PartnerCode).ToList();

        PropertyManagers = PropertyManagers.Join(
            properties,
            (a) => a.PartnerCode,
             (b) => b.PartnerCode,
            (a, b) => new { a, b })
            .Select(
                (ab, b) => new Partner
                {
                    Id = ab.a.Id,
                    PartnerCode = ab.a.PartnerCode,
                    Token = "",
                    Name = ab.a.Name,
                    Street = ab.a.Street,
                    PostalCode = ab.a.PostalCode,
                    City = ab.a.City,
                    MigrationStatus = PartnerMigrationStatus.PREPARED_FOR_WP
                }
            ).DistinctBy(a => a.PartnerCode).ToList();

        PropertyManagers.RemoveAll(a => ExistedPropertyManagers.Contains(a.PartnerCode));
        return PropertyManagers;
    }

    public void Initialize()
    {
        var list = EnrichPropertyManagers(LoadPropertyManagers());

        db.Partners.AddRange(list);
        db.SaveChanges();
    }
}