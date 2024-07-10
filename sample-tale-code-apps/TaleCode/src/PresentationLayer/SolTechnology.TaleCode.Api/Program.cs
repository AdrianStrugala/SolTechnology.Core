using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SolTechnology.Core.Api.Filters;
using SolTechnology.Core.Api.Middlewares;
using SolTechnology.Core.Authentication;
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
builder.Services.AddSingleton<IActionResultExecutor<ObjectResult>, ResponseEnvelopeResultExecutor>();

builder.Services.InstallQueries();


var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
builder.Services.AddControllers(opts =>
{
    opts.Filters.Add(authenticationFiler);
    opts.Filters.Add<ExceptionFilter>();
    opts.Filters.Add<ResponseEnvelopeFilter>();
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