using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SolTechnology.Core.ApiClient;
using SolTechnology.Core.Authentication;
using SolTechnology.TaleCode.PlayerRegistry.Commands;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();


//TODO: Messagebus can be refactored to have internal api like here in AddLogging
builder.Services.AddLogging(c =>
        c.AddConsole()
        .AddApplicationInsights());
builder.Services.AddApplicationInsightsTelemetry();


builder.Services.AddCommands();


var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
builder.Services.AddControllers(opts => opts.Filters.Add(authenticationFiler));


//SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });
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
