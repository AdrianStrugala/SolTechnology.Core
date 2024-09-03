using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.OpenApi.Models;
using Quartz;
using Quartz.Simpl;
using Quartz.Spi;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.Authentication;
using SolTechnology.Core.Logging.Middleware;
using SolTechnology.TaleCode.Api;
using SolTechnology.TaleCode.PlayerRegistry.Commands;
using SolTechnology.TaleCode.PlayerRegistry.Commands.CalculatePlayerStatistics;
using SolTechnology.TaleCode.PlayerRegistry.Queries;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();


//TODO: Message bus can be refactored to have internal fluent api like here in AddLogging
builder.Services.AddLogging(c =>
        c.AddConsole()
        .AddApplicationInsights());
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.InstallQueries();
builder.Services.InstallCommands();


var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
builder.Services.AddControllers(opts =>
{
    opts.Filters.Add(authenticationFiler);
    opts.Filters.Add<ExceptionFilter>();
    opts.Filters.Add<ResponseEnvelopeFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    options.JsonSerializerOptions.WriteIndented = true;
});


builder.Services.AddQuartz(q =>
{
    q.SchedulerName = "Scheduler-Core";

// these are the defaults
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });


    q.AddJob<CalculatePlayerStatisticsHandler>(j => j
        .WithIdentity("name", "group") // name "myJob", group "group1"
        .StoreDurably() // we need to store durably if no trigger is associated
        .WithDescription("my awesome job")
    );

});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});


//SWAGGER
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaleCode API", Version = "v1" });
    c.ExampleFilters();
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = SolTechnologyAuthenticationOptions.AuthenticationHeaderName,
        Description = "Authentication: Api Key for TaleCode"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            new string[] { }
        }
    });
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler("/error");
app.UseMiddleware<LoggingMiddleware>();


app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();
app.Run();


// Make the implicit Program class public so test projects can access it
public partial class Program { }