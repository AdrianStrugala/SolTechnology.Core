using SolTechnology.TaleCode.ApiClients.FootballDataApi;

var wireMock = new TaleCode.Faker.WireMockStartup();
wireMock.Run(2137, true);

wireMock
    .GetFakeApi<IFootballDataApiClient>()
    .WithRequest(x => x.GetPlayerById, priority:1)
    .WithResponse(x => x.WithSuccess().WithBody("Just test body"));

wireMock
    .GetFakeApi<IFootballDataApiClient>()
    .WithRequest(x => x.GetMatchById)
    .WithResponse(x => x.WithSuccess().WithBody("Dupa"));

Console.ReadKey();