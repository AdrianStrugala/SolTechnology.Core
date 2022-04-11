

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
 <a href="https://www.nuget.org/packages?q=SolTechnology"><img src="https://img.shields.io/badge/Version-v0.2-blue?logo=nuget"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core/actions"><img src="https://github.com/AdrianStrugala/SolTechnology.Core/actions/workflows/publishPackages.yml/badge.svg"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core"><img src="https://badgen.net/badge/%E2%AD%90Stars/%E2%98%852%E2%98%85/yellow"></a>

</p>


## Core Libraries


**The SolTechnology.Core repository contains a set of shared libraries. This is a foundation for CQRS driven applications using basic Azure technologies. The libraries support a modern coding approach presented in the example application called "Tale Code".**


| Documentation  | Nuget  |   |
|---|---|---|
|[Sql Database](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Sql.md)      |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Sql/"><img src="https://badgen.net/badge/Downloads/150/?icon=nuget"></a>  |   |
|[Blob Storage](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Blob.md)     |  <a href="https://www.nuget.org/packages/SolTechnology.Core.BlobStorage/"><img src="https://badgen.net/badge/Downloads/100/?icon=nuget"></a>  |   |
|[HttpClients](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Clients.md)   |  <a href="https://www.nuget.org/packages/SolTechnology.Core.ApiClient/"><img src="https://badgen.net/badge/Downloads/150/?icon=nuget"></a>  |   |
|[Message Bus](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Bus.md)       |  <a href="https://www.nuget.org/packages/SolTechnology.Core.MessageBus/"><img src="https://badgen.net/badge/Downloads/50/?icon=nuget"></a>  |   |
|[Guards](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Guards.md)         |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Guards/"><img src="https://badgen.net/badge/Downloads/100/?icon=nuget"></a>  |   |
|[Authentication](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Auth.md)   |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Authentication/"><img src="https://badgen.net/badge/Downloads/50/?icon=nuget"></a>  |   |
|[Logging](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Log.md)           |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Logging/"><img src="https://badgen.net/badge/Downloads/50/?icon=nuget"></a>  |   |
|[Scheduler](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Cron.md)        |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Scheduler/"><img src="https://badgen.net/badge/Downloads/0/?icon=nuget"></a>  |   |



## Tale Code


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