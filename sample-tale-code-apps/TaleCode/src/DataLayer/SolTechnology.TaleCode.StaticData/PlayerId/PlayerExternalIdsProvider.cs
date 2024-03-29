﻿namespace SolTechnology.TaleCode.StaticData.PlayerId
{
    public class PlayerExternalIdsProvider : IPlayerExternalIdsProvider
    {
        private static readonly Dictionary<int, PlayerIdMap> PlayerIdMap = new Dictionary<int, PlayerIdMap>
        {
            { 44, new PlayerIdMap {FootballDataId = 44, ApiFootballId = 874} }
        };

        public PlayerIdMap Get(int playerId)
        {
            PlayerIdMap.TryGetValue(playerId, out var result);

            if (result == null)
            {
                throw new ArgumentException($"Player [{playerId}] was not found. Please provide his Id's to StaticData Mapping");
            }

            return result;
        }
    }
}