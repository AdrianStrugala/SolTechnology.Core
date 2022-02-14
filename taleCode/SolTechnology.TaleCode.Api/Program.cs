using ApiClients;
using ApiClients.FootballDataApi;
using Microsoft.Extensions.Options;
using SolTechnology.Core.ApiClient;
using SolTechnology.TaleCode;
using SolTechnology.TaleCode.Infrastructure;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiClients();


builder.Services.AddScoped<ICommandHandler<SynchronizePlayerMatchesCommand>, SynchronizePlayerMatchesHandler>();
builder.Services.AddScoped<IFootballDataApiClient, FootballDataApiClient>();



var app = builder.Build();



//USE TO CHECK CONFIGURATION EXTENSION METHODS
var x = app.Services.GetService<IOptions<ApiClientConfiguration>>();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
