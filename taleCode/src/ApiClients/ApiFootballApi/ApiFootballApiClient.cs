using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;

namespace SolTechnology.TaleCode.ApiClients.ApiFootballApi
{
    public class ApiFootballApiClient : IApiFootballApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiFootballApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<List<string>> GetPlayerTeams(int apiId)
        {
            List<Team> result = new List<Team>();

            var apiResult = await _httpClient.GetAsync<TransferDetails>($"v3/transfers?player={apiId}");

            if (apiResult.Response != null)
            {
                var transfers = apiResult.Response.First().Transfers.OrderBy(t => t.Date).ToList();

                for (int i = 0; i < transfers.Count; i++)
                {
                    var transfer = transfers[i];

                    if (i == 0)
                    {
                        result.Add(new Team
                        {
                            Name = transfer.Teams.Out.Name,
                            TimeFrom = DateOnly.MinValue,
                            TimeTo = DateOnly.Parse(transfer.Date)
                        });
                    }

                    if (i != transfers.Count - 1)
                    {
                        result.Add(new Team
                        {
                            Name = transfer.Teams.Out.Name,
                            TimeFrom = DateOnly.Parse(transfer.Date),
                            TimeTo = DateOnly.Parse(transfers[i + 1].Date)
                        });
                    }
                    else
                    {
                        result.Add(new Team
                        {
                            Name = transfer.Teams.Out.Name,
                            TimeFrom = DateOnly.Parse(transfer.Date),
                            TimeTo = DateOnly.FromDateTime(DateTime.Now)
                        });
                    }
                }
            }

            var y = result;

            return new List<string>();
        }
    }

    public class Team
    {
        public string Name { get; set; }
        public DateOnly TimeFrom { get; set; }
        public DateOnly TimeTo { get; set; }
    }
}
