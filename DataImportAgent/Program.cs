using DataImportAgent.Logger;
using FileImportAgent.PropertyManagerAgent;
using Quartz;
using SharedKernel.Data;
using System.Globalization;

using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using WebnimbusDataImportAgent;
using Microsoft.EntityFrameworkCore;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DataImportAgent";
});

LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddDbContextFactory<VDMAdminDbContext>();
builder.Services.AddDbContextFactory<SAPDbContext>(options => options.UseSqlServer(SAPDbContext.GetConnectionString(builder.Configuration)));

builder.Services.AddHostedService<ScheduleRunner>();

// Configure the HTTP request pipeline.

var cultureInfo = new CultureInfo("en-US");
cultureInfo.NumberFormat.CurrencySymbol = "�";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

IHost host = builder.Build();
//VDMAdminDbContext.MigrateDatabase(host);

host.Run();