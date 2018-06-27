using System;
using System.Threading.Tasks;
using DreamTravel.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DreamTravel.Repositories
{
    public class SessionRepository
    {
        private readonly IDatabase _redisStore;

        public SessionRepository(IDatabase redisStore)
        {
            _redisStore = redisStore;
        }

        public async Task AddCache(Session session)
        {
            await _redisStore.StringSetAsync($"{nameof(Session)}:{session.Id}", JsonConvert.SerializeObject(session), TimeSpan.FromHours(24));
        }

        public async Task<Session> GetCache(string sessionId)
        {
            Session session = new Session();
            var resultJson = await _redisStore.StringGetAsync($"{nameof(Session)}:{sessionId}");

            if (!RedisValueNullOrEmpty(resultJson))
            {
                session =
                    JsonConvert.DeserializeObject<Session>(resultJson);
            }

            return session;
        }

        public Session WriteToSession(string sessionId, EvaluationMatrix matrices)
        {
            Session result = new Session
            {
                Id = sessionId,
                NoOfCities = (int)Math.Sqrt(matrices.FreeDistances.Length),
                FreeDistances = matrices.FreeDistances,
                TollDistances = matrices.TollDistances,
                Costs = matrices.Costs
            };

            return result;
        }

        private static bool RedisValueNullOrEmpty(RedisValue redisValue)
        {
            return redisValue == RedisValue.Null ||
                   redisValue == RedisValue.EmptyString;
        }

        public EvaluationMatrix ReadFromSession(Session session)
        {
            EvaluationMatrix matrices = new EvaluationMatrix(session.NoOfCities);
            matrices.FreeDistances = session.FreeDistances;
            matrices.Costs = session.Costs;
            matrices.TollDistances = session.Costs;

            return matrices;
        }
    }
}
