using Hangfire;
using LegalSearch.Api;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddServicesToContainer(configuration);
builder.Services.ConfigureInfrastructureServices(configuration);
var app = builder.Build();

app.ConfigureHttpRequestPipeline(configuration);

//jobs
//RecurringJob.AddOrUpdate<IBackgroundService>(x => x.CheckAndRerouteRequests(), Cron.Minutely);

app.Run();
