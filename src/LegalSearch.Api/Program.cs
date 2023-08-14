using Hangfire;
using LegalSearch.Api;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Infrastructure;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddServicesToContainer(configuration);
builder.Services.ConfigureInfrastructureServices(configuration);
var app = builder.Build();

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var context = serviceScope.ServiceProvider.GetService<AppDbContext>();

    //context!.Database.Migrate();
    //context.Database.EnsureCreated();
}

app.ConfigureHttpRequestPipeline(configuration);

//jobs
//RecurringJob.AddOrUpdate<IBackgroundService>(x => x.CheckAndRerouteRequests(), Cron.Minutely);

app.Run();
