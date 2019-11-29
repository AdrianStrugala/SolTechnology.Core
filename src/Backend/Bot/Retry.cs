using System;
using Microsoft.Azure.WebJobs;

namespace DreamTravel.Bot
{
    public static class Retry
    {
        private static readonly Random Random = new Random();

        public static RetryOptions Options => new RetryOptions(TimeSpan.FromSeconds(Random.Next(6, 16)), 3);
    }
}
