using LegalSearch.Api;
using LegalSearch.Infrastructure;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddServicesToContainer(configuration);
builder.Services.ConfigureInfrastructureServices(configuration);
var app = builder.Build();

app.ConfigureHttpRequestPipeline();
app.Run();
