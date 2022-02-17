using Microsoft.Extensions.Options;
using SolTechnology.Core.ApiClient;
using SolTechnology.Core.Sql;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSql();

builder.Services.AddCommands();



var app = builder.Build();



//USE TO CHECK CONFIGURATION EXTENSION METHODS
var x = app.Services.GetService<IOptions<ApiClientConfiguration>>();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
