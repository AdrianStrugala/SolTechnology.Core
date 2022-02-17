using ApiClients.FootballDataApi;
using SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Interfaces;
using Match = SolTechnology.TaleCode.Domain.Match;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches.Executors
{
    public class AssignWinner : IAssignWinner
    {
        private readonly IFootballDataApiClient _footballDataApiClient;

        public AssignWinner(IFootballDataApiClient footballDataApiClient)
        {
            _footballDataApiClient = footballDataApiClient;
        }

        public async Task Execute(Match match)
        {
            var clientMatch = await _footballDataApiClient.GetMatchById(match.ApiId);

            match.AssignCompetitionWinner(clientMatch.CompetitionWinner);
        }
    }
}
