namespace SolTechnology.TaleCode.StaticData.PlayerId
{
    public class PlayerIdProvider : IPlayerIdProvider
    {
        private static readonly Dictionary<string, PlayerIdMap> PlayerIdMap = new Dictionary<string, PlayerIdMap>
        {
            { "Cristiano Ronaldo", new PlayerIdMap {FootballDataId = 44, ApiFootballId = 874} }
        };

        public PlayerIdMap GetPlayerId(string name)
        {
            PlayerIdMap.TryGetValue(name, out var result);

            if (result == null)
            {
                throw new ArgumentException($"Player [{name}] was not found. Please provide his Id's to StaticData Mapping");
            }

            return result;
        }
    }
}