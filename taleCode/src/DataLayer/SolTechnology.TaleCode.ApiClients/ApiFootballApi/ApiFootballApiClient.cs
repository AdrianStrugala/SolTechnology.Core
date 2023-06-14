using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;
using SolTechnology.TaleCode.StaticData;

namespace SolTechnology.TaleCode.ApiClients.ApiFootballApi
{
    public class ApiFootballApiClient : IApiFootballApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiFootballApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<List<Team>> GetPlayerTeams(int apiId)
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
                            TimeFrom = DateProvider.DateMin(),
                            TimeTo = transfer.Date
                        });
                    }

                    if (i != transfers.Count - 1)
                    {
                        result.Add(new Team
                        {
                            Name = transfer.Teams.In.Name,
                            TimeFrom = transfer.Date,
                            TimeTo = transfers[i + 1].Date
                        });
                    }
                    else
                    {
                        result.Add(new Team
                        {
                            Name = transfer.Teams.In.Name,
                            TimeFrom = transfer.Date,
                            TimeTo = DateProvider.DateMax()
                        });
                    }
                }
            }

            return result;
        }
    }
}
