﻿using SolTechnology.Core.MessageBus;
using SolTechnology.Core.MessageBus.Publish;

namespace SolTechnology.TaleCode.PlayerRegistry.Commands.SynchronizePlayerMatches
{
    public class PlayerMatchesSynchronizedEvent : IMessage
    {
        public int PlayerId { get; set; }

        public PlayerMatchesSynchronizedEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}