using LegalSearch.Api;
using LegalSearch.Infrastructure;
using LegalSearch.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddServicesToContainer(configuration);
builder.Services.ConfigureInfrastructureServices(configuration);
var app = builder.Build();

app.ConfigureHttpRequestPipeline(configuration);

app.Run();
