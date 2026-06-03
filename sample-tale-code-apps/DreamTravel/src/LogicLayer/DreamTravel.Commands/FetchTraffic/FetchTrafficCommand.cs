﻿using SolTechnology.Core.CQRS;

namespace DreamTravel.Commands.FetchTraffic
{
    public class FetchTrafficCommand : ICommand
    {
        public DateTime DepartureTime { get; set; }
    }
}
