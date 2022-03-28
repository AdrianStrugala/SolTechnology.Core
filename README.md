

<p align="center">
    <img alt="SolTechnology-logo" src="./docs/logo.png" width="200">
</p>

<h2 align="center">
  SolTechnology.Core
</h2>

<p align="center">
 <a> Modern redable coding </a>
</p>

<p align="center">
 <a href="https://www.nuget.org/packages?q=SolTechnology"><img src="https://img.shields.io/badge/Nuget-v0.2-blue?logo=nuget"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core"><img src="https://img.shields.io/badge/Downloads-500-blue?logo=github"></a>
//build or coverage

</p>


## Libraries


**The SolTechnology.Core repository contains a set of shared libraries. This is a foundation for CQRS driven applications using basic Azure technologies. The libraries support a modern coding approach presented in the example application called "Tale Code".**


- [Sql Database](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Sql.md)
- [Blob Storage](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Blob.md)
- [HttpClients](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Clients.md)
- [Message Bus](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Bus.md)
- [Guards](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Guards.md)
- [Authentication](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Auth.md)
- [Logging](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Log.md)
- [Pipelines and resource templates](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/CICD.md)
- [TaleCode](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Tale.md)



## Tale Code

I will fill this one day




I. SolTechnology Core Libraries:




 - Blob:
IBlobConnectionFactory
IBlobContainerClientWrapper (read and write operation)


- HttpClient:
     services.AddApiClient<IFootballDataApiClient, FootballDataApiClient>("football-data");  //has to match the name from configuration

- Guards:
 * provides series of extension methods protecting arguments from invalid value (code contract pattern)
Guards.Method()


Authentication

IMPORTANT:
Requires authentication Key in config, invoking installation AND ADDING FILTER TO (MVC) OPTOONS 

var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
builder.Services.AddControllers(opts => opts.Filters.Add(authenticationFiler));


i jeszcze

  app.UseAuthentication();


II. TaleCode:

1) App idea

a) get matches of player by player id
http://api.football-data.org/v2/players/44/matches?limit=1

b) get match details by match id
https://api.football-data.org/v2/matches/327130

c) store match result

d) calculate how many matches of player led to team victory

2) Architecture

3) Infrastructure and Deployment

Infrastrucutre comes from Bicep (ARM Template overlay)
Deployed by yaml pipeline in 3 steps: Test, Build&Publish, Deploy

Configuration is tokenized. Values come from Pipelines->Library->VariableGroup


4) Tale Code Approach



5) Testing Piramide