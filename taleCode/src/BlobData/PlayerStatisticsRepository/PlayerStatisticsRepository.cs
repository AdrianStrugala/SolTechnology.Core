using Azure.Storage.Blobs;
using SolTechnology.Core.BlobStorage.BlobContainerWrapper;
using SolTechnology.Core.BlobStorage.Connection;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.BlobData.PlayerStatisticsRepository
{
    public class PlayerStatisticsRepository : IPlayerStatisticsRepository
    {
        private const string ContainerName = "playerstatistics";

        private readonly IBlobContainerClientWrapper _blobContainerClientWrapper;
        private readonly BlobContainerClient _client;

        //TODO Rework blob wrapper to extension methods. Data type as parameter

        public PlayerStatisticsRepository(IBlobConnectionFactory blobConnectionFactory, IBlobContainerClientWrapper blobContainerClientWrapper)
        {
            _blobContainerClientWrapper = blobContainerClientWrapper;

            _client = blobConnectionFactory.CreateConnection(ContainerName);
        }

        public async Task Add(PlayerStatistics playerStatistics)
        {
            await _blobContainerClientWrapper.WriteToBlob(_client, playerStatistics.Id.ToString(), playerStatistics);
        }

        public async Task<PlayerStatistics> Get(int playerId)
        {
            var result = await _blobContainerClientWrapper.ReadFromBlob<PlayerStatistics>(_client, playerId.ToString());

            return result;
        }
    }
}