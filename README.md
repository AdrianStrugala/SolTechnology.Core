# SolTechnology.Core



I will fill this one day


Table of Contents

I. SolTechnology Core Libraries:

- SQL

For SQL DB use:
ISqlConnectionFactory


 - Blob:
IBlobConnectionFactory
IBlobContainerClientWrapper (read and write operation)


- HttpClient:
     services.AddApiClient<IFootballDataApiClient, FootballDataApiClient>("football-data");  //has to match the name from configuration

- Guards:
 * provides series of extension methods protecting arguments from invalid value (code contract pattern)
Guards.Method()




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



