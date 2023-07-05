using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SolTechnology.Core.ApiClient;
using SolTechnology.Core.Authentication;
using SolTechnology.TaleCode.PlayerRegistry.Queries;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();


//TODO: Messagebus can be refactored to have internal api like here in AddLogging
builder.Services.AddLogging(c =>
        c.AddConsole()
        .AddApplicationInsights());
builder.Services.AddApplicationInsightsTelemetry();


builder.Services.InstallQueries();


var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
builder.Services.AddControllers(opts => opts.Filters.Add(authenticationFiler));


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



//USE TO CHECK CONFIGURATION EXTENSION METHODS
var x = app.Services.GetService<IOptions<ApiClientConfiguration>>();

app.UseSwagger();
app.UseSwaggerUI();


app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();
app.Run();


// Make the implicit Program class public so test projects can access it
public partial class Program { }